using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;

namespace Fleck
{
    public interface ISocket
    {
        bool Connected { get; }
        string RemoteIpAddress { get; }
        Stream Stream { get; }

        Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error);
        Task Send(byte[] buffer, Action callback, Action<Exception> error);
        Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset = 0);
        Task Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error);

        void Dispose();
        void Close();

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);
    }
}
