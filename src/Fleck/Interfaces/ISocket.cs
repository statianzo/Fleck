using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;
using System.Security.Authentication;

namespace Fleck
{
    public interface ISocket
    {
        bool Connected { get; }
        string RemoteIpAddress { get; }
        int RemotePort { get; }
        Stream Stream { get; }
        bool NoDelay { get; set; }

        Task<ISocket> AcceptAsync(Action<ISocket> callback, Action<Exception> error);
        Task SendAsync(byte[] buffer, Action callback, Action<Exception> error);
        Task<int> ReceiveAsync(byte[] buffer, Action<int> callback, Action<Exception> error, int offset = 0);
        Task AuthenticateAsync(X509Certificate2 certificate, SslProtocols enabledSslProtocols, Action callback, Action<Exception> error);

        void Dispose();
        void Close();

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);

        [Obsolete]
        Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error);
        [Obsolete]
        Task Send(byte[] buffer, Action callback, Action<Exception> error);
        [Obsolete]
        Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset = 0);
        [Obsolete]
        Task Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols, Action callback, Action<Exception> error);
    }
}
