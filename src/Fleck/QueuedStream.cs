using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Fleck
{
    /// <summary>
    /// Wraps a stream and queues multiple write operations.
    /// Useful for wrapping SslStream as it does not support multiple simultaneous write operations.
    /// </summary>
    public class QueuedStream : Stream
    {
        readonly Stream _stream;
        readonly Queue<WriteData> _queue = new Queue<WriteData>();
        int _pendingWrite;
        bool _disposed;

        public QueuedStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("QueuedStream does not support synchronous write operations yet.");
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            lock (_queue)
            {
                var data = new WriteData(buffer, offset, count, callback, state);
                if (_pendingWrite > 0)
                {
                    _queue.Enqueue(data);
                    return data.AsyncResult;
                }
                return BeginWriteInternal(buffer, offset, count, callback, state, data);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult is QueuedWriteResult)
            {
                var queuedResult = asyncResult as QueuedWriteResult;
                if (queuedResult.Exception != null) throw queuedResult.Exception;
                var ar = queuedResult.ActualResult;
                if (ar == null)
                {
                    throw new NotSupportedException(
                        "QueuedStream does not support synchronous write operations. Please wait for callback to be invoked before calling EndWrite.");
                }
                // EndWrite on actual stream should already be invoked.
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override void Close()
        {
            _stream.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _stream.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        IAsyncResult BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback callback, object state, WriteData queued)
        {
            _pendingWrite++;
    
            var result = _stream.BeginWrite(buffer, offset, count, ar =>
            {
                // if the operation completes in async mode, the callback will manage it
                if (!ar.CompletedSynchronously)
                {
                    ProcessComplete(ar, queued, callback);
                }
            }, state);

            queued.AsyncResult.ActualResult = result;

            // if it completes in not async mode, we could manage it on the current thread
            if (result.CompletedSynchronously)
            {
                ProcessComplete(result, queued, callback);
            }

            return queued.AsyncResult;
        }

        // this Method avoid the recursion of the stack
        private void ProcessComplete(IAsyncResult ar, WriteData queued, AsyncCallback callback)
        {
            queued.AsyncResult.ActualResult = ar;
            try
            {
                _stream.EndWrite(ar);
            }
            catch (Exception exc)
            {
                queued.AsyncResult.Exception = exc;
            }

            lock (_queue)
            {
                _pendingWrite--;
        
                while (_queue.Count > 0)
                {
                    var nextData = _queue.Dequeue();
                    _pendingWrite++;

                    try
                    {
                        var nextResult = _stream.BeginWrite(nextData.Buffer, nextData.Offset, nextData.Count, nextAr =>
                        {
                            if (!nextAr.CompletedSynchronously)
                            {
                                ProcessComplete(nextAr, nextData, nextData.Callback);
                            }
                        }, nextData.State);

                        nextData.AsyncResult.ActualResult = nextResult;

                        // if it is async, the runtine will exit from the loop
                        if (!nextResult.CompletedSynchronously)
                        {
                            break;
                        }
                
                        // if it is not async, not recursion
                        try
                        {
                            _stream.EndWrite(nextResult);
                        }
                        catch (Exception exc)
                        {
                            nextData.AsyncResult.Exception = exc;
                        }
                        _pendingWrite--;
                        nextData.Callback(nextData.AsyncResult);
                    }
                    catch (Exception exc)
                    {
                        _pendingWrite--;
                        nextData.AsyncResult.Exception = exc;
                        nextData.Callback(nextData.AsyncResult);
                    }
                }
        
                callback(queued.AsyncResult);
            }
        }

        #region Nested type: WriteData

        class WriteData
        {
            public readonly byte[] Buffer;
            public readonly int Offset;
            public readonly int Count;
            public readonly AsyncCallback Callback;
            public readonly object State;
            public readonly QueuedWriteResult AsyncResult;

            public WriteData(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                Buffer = buffer;
                Offset = offset;
                Count = count;
                Callback = callback;
                State = state;
                AsyncResult = new QueuedWriteResult(state);
            }
        }

        #endregion

        #region Nested type: QueuedWriteResult

        class QueuedWriteResult : IAsyncResult
        {
            readonly object _state;

            public QueuedWriteResult(object state)
            {
                _state = state;
            }

            public Exception Exception { get; set; }

            public IAsyncResult ActualResult { get; set; }

            public object AsyncState
            {
                get { return _state; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { throw new NotSupportedException("Queued write operations do not support wait handle."); }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return ActualResult != null && ActualResult.IsCompleted; }
            }
        }

        #endregion
    }
}
