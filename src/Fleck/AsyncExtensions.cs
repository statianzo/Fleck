using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fleck
{
#if NET40

    internal static class AsyncExtensions
    {
        public static Task AuthenticateAsServerAsync(this SslStream stream,
                                                     X509Certificate serverCertificate,
                                                     bool clientCertificateRequired,
                                                     SslProtocols enabledSslProtocols,
                                                     bool checkCertificateRevocation)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => stream.BeginAuthenticateAsServer
                    (serverCertificate,
                     clientCertificateRequired, enabledSslProtocols,
                     checkCertificateRevocation, cb, s);

            Task task = Task.Factory.FromAsync(begin, stream.EndAuthenticateAsServer, null);

            return task;
        }

        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => stream.BeginRead(buffer, offset, count, cb, s);

            Task<int> task = Task.Factory.FromAsync(begin, stream.EndRead, null);

            return task;
        }

        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => stream.BeginWrite(buffer, offset, count, cb, s);

            Task task = Task.Factory.FromAsync(begin, stream.EndWrite, null);

            return task;
        }

        public static Task<Socket> AcceptAsync(this Socket socket)
        {
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => socket.BeginAccept(cb, s);

            Task<Socket> task = Task.Factory.FromAsync(begin, socket.EndAccept, null);

            return task;
        }
    }

#endif
}