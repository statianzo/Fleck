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
                Stream = new NetworkStream(_socket);
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

        public Stream Stream { get; private set; }

        public void Close()
        {
            if (Stream != null) Stream.Close();
            if (_socket != null) _socket.Close();
        }

        public void Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset)
        {
            if (Stream == null) return;

            Task.Factory.StartNew(
                () => RunAsync((cb, s) => Stream.BeginRead(buffer, offset, buffer.Length, cb, s),
                               Stream.EndRead,
                               callback,
                               error));
        }

        public void Accept(Action<ISocket> callback, Action<Exception> error)
        {
            Task.Factory.StartNew(
                () => RunAsync(_socket.BeginAccept,
                               socket => new SocketWrapper(_socket.EndAccept(socket)),
                               callback,
                               error));
        }

        public void Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            Task.Factory.StartNew(
                () => RunAsync((cb, s) => Stream.BeginWrite(buffer, 0, buffer.Length, cb, s),
                               Stream.EndWrite,
                               callback,
                               error));
        }

        public void Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
            if (Stream == null) return;

            var ssl = new SslStream(Stream, false);
            Stream = ssl;

            Task.Factory.StartNew(
                () => RunAsync((cb, s) => ssl.BeginAuthenticateAsServer(certificate, false, SslProtocols.Tls, false, cb, s),
                               ssl.EndAuthenticateAsServer,
                               callback,
                               error));
        }

        private void RunAsync<T>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, T> end, Action<T> callback, Action<Exception> error)
        {
            try
            {
                if (_socket == null) return;

                begin(ar =>
                          {
                              var result = default(T);

                              try
                              {
                                  if (_socket == null) return;

                                  result = end(ar);
                              }
                              catch (ObjectDisposedException)
                              {
                              }
                              catch (IOException)
                              {
                              }
                              catch (Exception ex)
                              {
                                  error(ex);
                              }

                              try
                              {
                                  callback(result);
                              }
                              catch (Exception ex)
                              {
                                  error(ex);
                                  throw;
                              }
                          }, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                error(ex);
            }
        }

        private void RunAsync(Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end, Action callback, Action<Exception> error)
        {
            try
            {
                if (_socket == null) return;

                begin(ar =>
                          {
                              try
                              {
                                  if (_socket == null) return;
                                  end(ar);
                              }
                              catch (ObjectDisposedException)
                              {
                              }
                              catch (IOException)
                              {
                              }
                              catch(Exception ex)
                              {
                                  error(ex);
                              }

                              try
                              {
                                  callback();
                              }
                              catch (Exception ex)
                              {
                                  error(ex);
                              }
                          }, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                error(ex);
            }
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
                if (Stream != null) Stream.Dispose();
                if (_socket != null) _socket.Dispose();

                _socket = null;
                Stream = null;
            }

            _disposed = true;
        }
    }
}