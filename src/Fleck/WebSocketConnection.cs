using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Fleck
{
    public class WebSocketConnection : IWebSocketConnection
    {
        private const int ReadSize = 8 * 1024;
        private const int MaxSendsPerPump = 64;
        private const int HandshakePending = 0;
        private const int HandshakeComplete = 1;
        private const int HandshakeAborted = 2;
        private static readonly byte[] HeadersEnd = { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        public ISocket Socket { get; set; }
        public IHandler Handler { get; set; }
        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnMessage { get; set; }
        public BinaryDataHandler OnBinary { get; set; }
        public BinaryDataHandler OnPing { get; set; }
        public BinaryDataHandler OnPong { get; set; }
        public Action<Exception> OnError { get; set; }
        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }
        public bool IsAvailable => !_closing && !_closed && Socket.Connected;
        public int MaxPendingSends { get; set; } = 2048;

        private readonly Action<IWebSocketConnection> _initialize;
        private readonly Func<IWebSocketConnection, WebSocketHttpRequest, IHandler> _handlerFactory;
        private readonly Func<ArraySegment<byte>, WebSocketHttpRequest> _parseRequest;
        private int _pendingSendCount;

        private static readonly ConcurrentBag<SendState> _sendStatePool = new ConcurrentBag<SendState>();
        private static readonly AsyncCallback _onSendComplete = OnSendComplete;
        private static readonly WaitCallback _pumpSends = PumpSendsSafely;
        private static readonly AsyncCallback _onReadComplete = OnReadComplete;
        private static readonly WaitCallback _resumeReading = ResumeReading;
        private const int MaxReadsPerLoop = 64;
        private int _resumeReadOffset;
        [ThreadStatic] private static int _pumpDepth;
        private readonly ConcurrentQueue<SendState> _sendQueue = new ConcurrentQueue<SendState>();
        private int _writing; // 1 while a thread is writing or a write is in flight
        private byte[] _receiveBuffer;
        private IHandler _receiveHandler; // the read loop's reference, Handler is cleared on close to stop sends
        private int _receiveReleased;
        private int _receiveOffset;
        private bool _closing;
        private bool _closed;
        private int _limiterReleased;
        private int _socketClosed;
        private int _closeStarted;
        private int _handshakeState;
        private int _handshakeQueued;
        private Timer _handshakeTimer;
        private static readonly TimeSpan MaxTimerDueTime = TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        private readonly ConnectionLimiter _limiter;
        private readonly bool _isAsync;

        public WebSocketConnection(
            ISocket socket,
            Action<IWebSocketConnection> initialize,
            Func<ArraySegment<byte>, WebSocketHttpRequest> parseRequest,
            Func<IWebSocketConnection, WebSocketHttpRequest, IHandler> handlerFactory,
            ConnectionLimiter limiter,
            TimeSpan? handshakeTimeout = null,
            bool isAsync = true)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            OnBinary = x => { };
            OnPing = x => { };
            OnPong = x => { };
            OnError = x => { };
            _initialize = initialize;
            _handlerFactory = handlerFactory;
            _parseRequest = parseRequest;
            _limiter = limiter;
            _isAsync = isAsync;

            // a client that never finishes its handshake (e.g. sending it a byte at a time) would otherwise hold its connection
            // and limiter slot forever. started here so it also covers a slow TLS negotiation
            // anything Timer can't schedule (about 49.7 days or more, e.g. TimeSpan.MaxValue) means no timeout, like zero does
            if (handshakeTimeout is TimeSpan timeout && timeout > TimeSpan.Zero && timeout < MaxTimerDueTime)
                _handshakeTimer = new Timer(s => ((WebSocketConnection)s).OnHandshakeTimeout(), this, timeout, Timeout.InfiniteTimeSpan);
        }

        public void Send(string message)
        {
            var handler = HandlerForSend(default);
            if (handler != null)
                SendImpl(handler.FrameText(message));
        }

        public void SendText(MemoryBuffer utf8StringBytes)
        {
            var handler = HandlerForSend(utf8StringBytes);
            if (handler != null)
                SendImpl(handler.FrameText(utf8StringBytes));
        }

        public void Send(MemoryBuffer message)
        {
            var handler = HandlerForSend(message);
            if (handler != null)
                SendImpl(handler.FrameBinary(message));
        }

        // sends the prefix and message as one binary frame, so a caller adding its own header doesn't have to copy the message first
        public void Send(ReadOnlySpan<byte> prefix, MemoryBuffer message)
        {
            var handler = HandlerForSend(message);
            if (handler != null)
                SendImpl(handler.FrameBinary(prefix, message));
        }

        public void SendPing(MemoryBuffer message)
        {
            var handler = HandlerForSend(message);
            if (handler != null)
                SendImpl(handler.FramePing(message));
        }

        public void SendPong(MemoryBuffer message)
        {
            var handler = HandlerForSend(message);
            if (handler != null)
                SendImpl(handler.FramePong(message));
        }

        // Handler is cleared when the connection closes, which can happen on another thread at any time, so read it once.
        // returns null when it can't be sent, in which case the message (which the caller hands over) is disposed here,
        // the same as framing it would have. sending after close is ignored the same as in SendImpl
        private IHandler HandlerForSend(MemoryBuffer message)
        {
            // Handler is set before the application's config callback runs, but anything sent before the handshake response
            // has been queued would go out ahead of it and break the connection
            var handler = Handler;
            if (handler != null && Volatile.Read(ref _handshakeQueued) != 0)
                return handler;

            message.Dispose();

            if (handler != null)
                throw new InvalidOperationException("Cannot send before handshake, send from OnOpen instead");

            if (Volatile.Read(ref _socketClosed) == 0)
                throw new InvalidOperationException("Cannot send before handshake");

            WarnQuietly("Data sent while closing or after close. Ignoring.");
            return null;
        }

        private void SendImpl(MemoryBuffer buffer)
        {
            if (!IsAvailable)
            {
                WarnQuietly("Data sent while closing or after close. Ignoring.");
                buffer.Dispose();
                return;
            }

            SendBytes(buffer);
        }

        public void Close()
        {
            Close(WebSocketStatusCodes.NormalClosure);
        }

        public void Close(ushort code)
        {
            // the read loop (answering a close or a protocol error) and the application can close at the same time,
            // so make sure only one close frame goes out
            if (_closing || _closed || Interlocked.Exchange(ref _closeStarted, 1) != 0)
                return;

            _closing = true;

            // a close frame only makes sense once the client has the handshake response, before that it would just be
            // garbage on an HTTP connection (e.g. when the application's config callback throws while setting up)
            var handler = Handler;
            if (handler == null || Volatile.Read(ref _handshakeState) != HandshakeComplete || !Socket.Connected)
            {
                CloseSocket();
                return;
            }

            var bytes = handler.FrameClose(code);
            if (bytes.Length == 0)
                CloseSocket();
            else
                SendBytes(bytes, (i, s) => i.CloseSocket());
        }

        public bool CreateHandler(ArraySegment<byte> data)
        {
            var request = _parseRequest(data);
            if (request == null)
                return false;

            // a local reference, since closing (e.g. the handshake timeout firing on another thread) clears Handler
            var handler = _handlerFactory(this, request);
            Handler = handler;
            if (handler == null)
                return false;

            _receiveHandler = handler;

            ConnectionInfo = WebSocketConnectionInfo.Create(request, Socket.RemoteIpAddress, Socket.RemotePort);

            _initialize(this);

            // closed while setting up: there's no one to send the handshake to, and the read loop's next check of the closed
            // socket releases its buffers
            if (Volatile.Read(ref _socketClosed) != 0)
                return false;

            // queued first, then sends are allowed (they can only go after it), then written
            var handshake = handler.CreateHandshake();
            SendBytes(handshake, (instance, success) =>
            {
                try
                {
                    if(!instance._isAsync)
                    {
                        Volatile.Write(ref _handshakeQueued, 1);
                    }

                    // Completion and timeout must choose one outcome before either raises application events.
                    if (success && Interlocked.CompareExchange(ref instance._handshakeState, HandshakeComplete, HandshakePending) == HandshakePending)
                    {
                        instance.StopHandshakeTimer();
                        instance.OnOpen();
                    }
                }
                finally
                {
                    // the read loop carries on from here rather than straight after queuing the handshake, so OnOpen always
                    // runs before OnMessage. An aborted handshake must not read even if the timeout is still closing it.
                    if (Volatile.Read(ref instance._handshakeState) == HandshakeComplete)
                        instance.Receive(0);
                    else
                        instance.ReleaseReceiveResources();
                }
            }, pump: false);

            if (_isAsync)
            {
                Volatile.Write(ref _handshakeQueued, 1);
                PumpSends();
            }

            return true;
        }

        // for the client: the handshake has already been done on the socket, so go straight to open. leftover is anything the
        // server sent after its handshake response, which belongs to the first frames
        internal void StartClient(IHandler handler, IWebSocketConnectionInfo info, ArraySegment<byte> leftover)
        {
            if (Volatile.Read(ref _socketClosed) != 0)
             {
                 handler.Dispose();
                 return;
             }

            ConnectionInfo = info;
            Handler = handler;
            _receiveHandler = handler;
            _receiveBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);

            Volatile.Write(ref _handshakeState, HandshakeComplete);
            Volatile.Write(ref _handshakeQueued, 1);

            try
            {
                OnOpen();

                if (leftover.Count > 0 && IsAvailable)
                    handler.Receive(new Span<byte>(leftover.Array, leftover.Offset, leftover.Count));
            }
            catch (Exception e)
            {
                HandleReadErrorSafely(e);
                return;
            }

            Receive(0);
        }

        private void OnHandshakeTimeout()
        {
            try
            {
                if (Interlocked.CompareExchange(ref _handshakeState, HandshakeAborted, HandshakePending) != HandshakePending)
                    return;

                try
                {
                    FleckLog.Debug("Handshake not completed in time. Closing.");
                }
                finally
                {
                    CloseSocket();
                }
            }
            catch (Exception e)
            {
                LogUnexpected("Error closing a connection whose handshake timed out", e);
            }
        }

        private void StopHandshakeTimer()
        {
            Interlocked.Exchange(ref _handshakeTimer, null)?.Dispose();
        }

        public void StartReceiving()
        {
            if (!IsAvailable)
                return;

            if (_receiveBuffer == null)
                _receiveBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);

            Receive(0);
        }

        // returns where in the receive buffer to read into next, or -1 when this read loop stops: the connection closed, or
        // the handshake was started, whose completion carries on reading (after OnOpen)
        private int HandleReadSuccess(int bytesRead)
        {
            if (bytesRead <= 0)
            {
                FleckLog.Debug("0 bytes read. Closing.");
                CloseSocket();
                ReleaseReceiveResources();
                return -1;
            }

            FleckLog.Debug($"{bytesRead} bytes read");

            var readBytes = new Span<byte>(_receiveBuffer, 0, bytesRead);

            if (_receiveHandler != null)
            {
                _receiveHandler.Receive(readBytes);
                return 0;
            }

            var searchFrom = Math.Max(0, _receiveOffset - (HeadersEnd.Length - 1));
            _receiveOffset += bytesRead;

            var end = new ReadOnlySpan<byte>(_receiveBuffer, searchFrom, _receiveOffset - searchFrom).IndexOf(HeadersEnd);
            if (end < 0)
                return _receiveOffset;

            if (!CreateHandler(new ArraySegment<byte>(_receiveBuffer, 0, searchFrom + end + HeadersEnd.Length)))
            {
                FleckLog.Debug("Invalid handshake request. Closing.");
                CloseSocket();
                ReleaseReceiveResources();
            }

            return -1;
        }

        private void HandleReadError(Exception e)
        {
            if (e is AggregateException)
            {
                var agg = e as AggregateException;
                HandleReadError(agg.InnerException);
                return;
            }

            // the read loop stops here either way
            try
            {
                if (e is ObjectDisposedException)
                {
                    FleckLog.Warn("Swallowing ObjectDisposedException", e);
                    return;
                }

                RaiseError(e);

                if (e is WebSocketException)
                {
                    FleckLog.Debug("Error while reading", e);
                    Close(((WebSocketException)e).StatusCode);
                }
                else if (e is IOException)
                {
                    // the transport failed, so there's no close handshake to do (and 1006 must never be sent in a close frame)
                    FleckLog.Debug("Error while reading", e);
                    CloseSocket();
                }
                else
                {
                    FleckLog.Error("Application Error", e);
                    Close(WebSocketStatusCodes.InternalServerError);
                }
            }
            finally
            {
                ReleaseReceiveResources();
            }
        }

        // Reads that complete synchronously (the data was already waiting) are handled in this loop rather than in the callback,
        // so a client that keeps sending can't recurse the stack until it overflows. After MaxReadsPerLoop of them in a row the
        // rest is handed to the thread pool, so one busy connection doesn't keep this thread to itself. Doesn't throw.
        private void Receive(int offset)
        {
            for (var reads = 0; offset >= 0; reads++)
            {
                // CloseSocket can run on another thread (e.g. when a send fails or the close frame finishes sending) while
                // this loop is handling data, so stop here instead of reading into a connection that's already gone
                if (Volatile.Read(ref _socketClosed) != 0)
                {
                    ReleaseReceiveResources();
                    return;
                }

                // not reading at all (StartReceiving was never called, e.g. a connection driven directly in tests)
                var buffer = _receiveBuffer;
                if (buffer == null)
                    return;

                if (reads == MaxReadsPerLoop)
                {
                    _resumeReadOffset = offset;
                    ThreadPool.UnsafeQueueUserWorkItem(_resumeReading, this);
                    return;
                }

                IAsyncResult result;
                try
                {
                    result = Socket.Stream.BeginRead(buffer, offset, buffer.Length - offset, _onReadComplete, this);
                }
                catch (Exception e)
                {
                    HandleReadErrorSafely(e);
                    return;
                }

                if (!result.CompletedSynchronously)
                    return; // OnReadComplete carries on

                offset = FinishRead(result);
            }
        }

        private static void OnReadComplete(IAsyncResult result)
        {
            // Receive handles reads that completed synchronously
            if (result.CompletedSynchronously)
                return;

            // on an IO thread with nothing above it, so an exception escaping would take the process down
            try
            {
                var instance = (WebSocketConnection)result.AsyncState;
                var next = instance.FinishRead(result);
                if (next >= 0)
                    instance.Receive(next);
            }
            catch (Exception e)
            {
                LogUnexpected("Error reading", e);
            }
        }

        private static void ResumeReading(object state)
        {
            try
            {
                var instance = (WebSocketConnection)state;
                instance.Receive(instance._resumeReadOffset);
            }
            catch (Exception e)
            {
                LogUnexpected("Error reading", e);
            }
        }

        // handles a completed read, returning where to read next or -1 to stop (see HandleReadSuccess). Doesn't throw
        private int FinishRead(IAsyncResult result)
        {
            try
            {
                return HandleReadSuccess(Socket.Stream.EndRead(result));
            }
            catch (Exception e)
            {
                HandleReadErrorSafely(e);
                return -1;
            }
        }

        // the read callback runs on an IO thread with nothing above it, so nothing may escape from here: if handling the error
        // throws (e.g. the application's LogAction), make sure the connection still goes away
        private void HandleReadErrorSafely(Exception e)
        {
            try
            {
                HandleReadError(e);
            }
            catch (Exception handlingError)
            {
                LogUnexpected("Error handling a failed read", handlingError);

                try
                {
                    CloseSocket();
                }
                catch (Exception closeError)
                {
                    LogUnexpected("Error closing after a failed read", closeError);
                }
            }
        }

        private void HandleWriteError(Exception e)
        {
            // close even if the application's logging throws
            try
            {
                if (e is ObjectDisposedException)
                {
                    FleckLog.Warn("Swallowing ObjectDisposedException", e);
                    return;
                }

                if (e is IOException)
                    FleckLog.Debug("Failed to send. Disconnecting.", e);
                else
                    FleckLog.Info("Failed to send. Disconnecting.", e);

                RaiseError(e);
            }
            finally
            {
                CloseSocket();
            }
        }

        // the application's handler throwing mustn't stop the connection from being cleaned up
        private void RaiseError(Exception e)
        {
            try
            {
                OnError(e);
            }
            catch (Exception handlerException)
            {
                LogUnexpected("OnError handler threw", handlerException);
            }
        }

        private void SendBytes(MemoryBuffer bytes, Action<WebSocketConnection, bool> callback = null, bool pump = true)
        {
            if (_isAsync)
            {
                if (Interlocked.Increment(ref _pendingSendCount) > MaxPendingSends && MaxPendingSends > 0)
                {
                    DecrementPending(); // never queued
                    bytes.Dispose();

                    try
                    {
                        HandleWriteError(new InvalidOperationException("Too many pending sends on WebSocket connection. Disconnecting."));
                    }
                    catch (Exception e)
                    {
                        LogUnexpected("Error handling a failed send", e); // keep Send from throwing
                    }

                    return;
                }

                // The completion callback is a cached static delegate and the per-send data (this connection, the buffer and the callback)
                // is carried on a pooled state object passed as AsyncState, so no alloc on the send path
                if (!_sendStatePool.TryTake(out var state))
                {
                    state = new SendState();
                }

                state.Connection = this;
                state.Bytes = bytes;
                state.Callback = callback;

                _sendQueue.Enqueue(state);

                if (pump)
                    PumpSends();
            }
            else
            {
                bool success = true;
                try
                {
                    Socket.Stream.Write(bytes.Data, 0, bytes.Length);
                }
                catch(Exception e)
                {
                    HandleWriteError(e);
                    success = false;
                }

                try
                {
                    bytes.Dispose();
                }
                catch(Exception e)
                {
                    LogUnexpected("Error releasing a sent buffer", e);
                }

                try
                {
                    callback?.Invoke(this, success);
                }
                catch(Exception e)
                {
                    RaiseError(e);
                }
            }
        }

        // Writes go out one at a time: a stream doesn't support overlapping writes (NetworkStream can split one into several
        // socket sends, letting another thread's frame land in the middle and corrupting the connection). Whichever thread
        // finds no write in flight takes the writer role for one send, then gives it up. A write still in flight keeps the
        // role until OnSendComplete. Synchronous completions are handled in this loop rather than in the callback, so a long
        // queue doesn't recurse. Callbacks and error handling (which run application code, e.g. OnOpen, OnClose, OnError)
        // only run once the role has been given up, so anything they send goes straight out instead of piling up behind them.
        // One call writes at most MaxSendsPerPump sends before handing the rest to the thread pool, so a thread that happens
        // to take the role (e.g. a game's main loop sending a reply) isn't kept writing what other threads keep queuing.
        private void PumpSends()
        {
            _pumpDepth++;
            try
            {
                var sends = 0;

                while (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
                {
                    var hadSend = false;
                    var stillWriting = false;
                    SendCompletion completion = default;

                    try
                    {
                        if (_sendQueue.TryDequeue(out var state))
                        {
                            hadSend = true;

                            if (Volatile.Read(ref _socketClosed) != 0)
                            {
                                completion = FinishSend(state, null, null); // drop it, the connection is gone
                            }
                            else
                            {
                                IAsyncResult result = null;
                                Exception error = null;
                                try
                                {
                                    result = Socket.Stream.BeginWrite(state.Bytes.Data, 0, state.Bytes.Length, _onSendComplete, state);
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                }

                                if (result != null && !result.CompletedSynchronously)
                                {
                                    stillWriting = true; // OnSendComplete takes over
                                    return;
                                }

                                completion = FinishSend(state, result, error);
                            }
                        }
                    }
                    finally
                    {
                        if (!stillWriting)
                            Volatile.Write(ref _writing, 0);
                    }

                    if (hadSend)
                    {
                        RunCompletion(completion);

                        if (++sends >= MaxSendsPerPump)
                        {
                            if (!_sendQueue.IsEmpty)
                                ThreadPool.UnsafeQueueUserWorkItem(_pumpSends, this);

                            return;
                        }

                        continue;
                    }

                    // a send queued after the queue looked empty but before writing was released would be stranded otherwise
                    if (_sendQueue.IsEmpty)
                        return;
                }
            }
            finally
            {
                _pumpDepth--;
            }
        }

        private static void OnSendComplete(IAsyncResult result)
        {
            // PumpSends finishes writes that completed synchronously
            if (result.CompletedSynchronously)
                return;

            var state = (SendState)result.AsyncState;
            var instance = state.Connection;

            // this runs on an IO thread with nothing above it, so an exception escaping would take the process down
            try
            {
                SendCompletion completion;
                try
                {
                    completion = instance.FinishSend(state, result, null);
                }
                finally
                {
                    Volatile.Write(ref instance._writing, 0);
                }

                instance.RunCompletion(completion);
            }
            catch (Exception e)
            {
                LogUnexpected("Error completing a send", e);
            }

            // some streams (e.g. QueuedStream over SslStream) run the callback inline from BeginWrite without reporting it as
            // synchronous, so continuing here would recurse through the queue. hand that case off to the thread pool instead
            if (_pumpDepth > 0)
                ThreadPool.UnsafeQueueUserWorkItem(_pumpSends, instance);
            else
                PumpSendsSafely(instance);
        }

        // for pumping on IO and thread pool threads, where an exception would take the process down
        private static void PumpSendsSafely(object connection)
        {
            try
            {
                ((WebSocketConnection)connection).PumpSends();
            }
            catch (Exception e)
            {
                LogUnexpected("Error sending", e);
            }
        }

        // for warnings on paths the application calls directly (e.g. Send), so a throwing LogAction can't escape into its code
        private static void WarnQuietly(string message)
        {
            try
            {
                FleckLog.Warn(message);
            }
            catch
            {
                // the application's LogAction threw
            }
        }

        private static void LogUnexpected(string message, Exception e)
        {
            try
            {
                FleckLog.Error(message, e);
            }
            catch
            {
                // the application's LogAction threw, there's nowhere left to report it
            }
        }

        // Ends the write and releases its resources, while the caller still has the writer role. Doesn't run any application
        // code: the returned completion does that via RunCompletion, once the role has been given up.
        // result is null when the write never started: error says why, or neither is set when it was dropped after close
        private SendCompletion FinishSend(SendState state, IAsyncResult result, Exception error)
        {
            var bytes = state.Bytes;
            var callback = state.Callback;

            state.Connection = null;
            state.Callback = null;
            state.Bytes = default;
            _sendStatePool.Add(state);

            var success = false;
            if (result != null)
            {
                try
                {
                    Socket.Stream.EndWrite(result);
                    success = true;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            try
            {
                bytes.Dispose();
            }
            catch (Exception e)
            {
                LogUnexpected("Error releasing a sent buffer", e); // still finish, so the callback (e.g. closing the socket) runs
            }

            DecrementPending();

            return new SendCompletion(callback, success, error);
        }

        // doesn't throw: it runs from Send, IO threads and the thread pool
        private void RunCompletion(SendCompletion completion)
        {
            try
            {
                if (completion.Error != null)
                    HandleWriteError(completion.Error);
            }
            catch (Exception e)
            {
                LogUnexpected("Error handling a failed send", e);
            }

            if (completion.Callback == null)
                return;

            try
            {
                completion.Callback(this, completion.Success);
            }
            catch (Exception e)
            {
                RaiseError(e);
            }
        }

        private readonly struct SendCompletion
        {
            public readonly Action<WebSocketConnection, bool> Callback;
            public readonly bool Success;
            public readonly Exception Error;

            public SendCompletion(Action<WebSocketConnection, bool> callback, bool success, Exception error)
            {
                Callback = callback;
                Success = success;
                Error = error;
            }
        }

        private void DecrementPending()
        {
            if (Interlocked.Decrement(ref _pendingSendCount) < 0)
            {
                _pendingSendCount = 0;
                LogUnexpected("Pending send count on WebSocket connection has gone negative! Trying to fix it...", null);
            }
        }

        private sealed class SendState
        {
            public WebSocketConnection Connection;
            public MemoryBuffer Bytes;
            public Action<WebSocketConnection, bool> Callback;
        }

        internal void ReleaseLimiterSlot()
        {
            if (Interlocked.Exchange(ref _limiterReleased, 1) != 0)
            {
                return;
            }

            // if keepalive has kicked in this may be a dead socket, in which case we'll want to fallback to connectioninfo
            var address = Socket.RemoteIpAddress ?? ConnectionInfo?.ClientIpAddress;
            _limiter?.Remove(address);
        }

        private void CloseSocket()
        {
            // A pending handshake completion must not open a connection we're closing.
            Interlocked.CompareExchange(ref _handshakeState, HandshakeAborted, HandshakePending);

            if (Interlocked.Exchange(ref _socketClosed, 1) != 0)
                return;

            ReleaseLimiterSlot();

            _closing = true;

            try
            {
                OnClose();
            }
            catch (Exception e)
            {
                // still close the socket
                LogUnexpected("OnClose handler threw", e);
            }

            _closed = true;

            // _socketClosed is already set, so nothing will try again if this doesn't finish
            try
            {
                Socket.Close();
            }
            catch (Exception e)
            {
                LogUnexpected("Error closing the socket", e);
            }

            try
            {
                Socket.Dispose();
            }
            catch (Exception e)
            {
                LogUnexpected("Error disposing the socket", e);
            }

            _closing = false;

            // don't reset _pendingSendCount here, sends still in flight complete (with errors) after this and decrement it

            // stops further sends. the handler's buffers and the receive buffer belong to the read loop, which may still be
            // using them on another thread, so it releases them when it stops (which disposing the socket guarantees)
            Handler = null;

            StopHandshakeTimer();
        }

        private void ReleaseReceiveResources()
        {
            if (Interlocked.Exchange(ref _receiveReleased, 1) != 0)
                return;

            _receiveHandler?.Dispose();
            _receiveHandler = null;

            if (_receiveBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_receiveBuffer);
                _receiveBuffer = null;
            }
        }
    }
}
