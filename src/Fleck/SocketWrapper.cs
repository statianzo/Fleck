namespace Fleck
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class SocketWrapper : ISocket
    {
        private Socket _socket;
        private Stream _stream;
        private bool _disposed;

        public string RemoteIpAddress
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Address.ToString() : null;
            }
        }

        public SocketWrapper(Socket socket)
        {
            _socket = socket;
            if (_socket.Connected)
                _stream = new NetworkStream(_socket);
        }

        public Task Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
            var ssl = new SslStream(_stream, false);
            _stream = ssl;
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => ssl.BeginAuthenticateAsServer(certificate, false, SslProtocols.Tls, false, cb, s);

            Task task = Task.Factory.FromAsync(begin, ssl.EndAuthenticateAsServer, null);
            task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public void Bind(EndPoint endPoint)
        {
            _socket.Bind(endPoint);
        }

        public bool Connected
        {
            get { return _socket != null && _socket.Connected; }
        }

        public Stream Stream
        {
            get { return _stream; }
        }

        public Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => _stream.BeginRead(buffer, offset, buffer.Length, cb, s);

            var task = Task.Factory.FromAsync<int>(begin, _stream.EndRead, null);
            task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error)
        {
            Func<IAsyncResult, ISocket> end = r => _socket == null ? null : new SocketWrapper(_socket.EndAccept(r));

            var task = Task.Factory.FromAsync(_socket.BeginAccept, end, null);
            task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SocketWrapper()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (_stream != null) _stream.Dispose();
                if (_socket != null) _socket.Dispose();

                _socket = null;
                _stream = null;
            }

            _disposed = true;
        }

        public void Close()
        {
            if (_stream != null) _stream.Close();
            if (_socket != null) _socket.Close();
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
            return 0;
        }

        public Task Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => _stream.BeginWrite(buffer, 0, buffer.Length, cb, s);

            var task = Task.Factory.FromAsync(begin, _stream.EndWrite, null);
            task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }
    }
}