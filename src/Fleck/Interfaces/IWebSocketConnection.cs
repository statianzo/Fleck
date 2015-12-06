using System;
using System.Threading.Tasks;

namespace Fleck
{
    public interface IWebSocketConnection
    {
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }
        Action<byte[]> OnBinary { get; set; }
        Action<byte[]> OnPing { get; set; }
        Action<byte[]> OnPong { get; set; }
        Action<Exception> OnError { get; set; }
        Task SendAsync(string message);
        Task SendAsync(byte[] message);
        Task SendPingAsync(byte[] message);
        Task SendPongAsync(byte[] message);
        void Close();
        IWebSocketConnectionInfo ConnectionInfo { get; }
        bool IsAvailable { get; }

        [Obsolete]
        Task Send(string message);
        [Obsolete]
        Task Send(byte[] message);
        [Obsolete]
        Task SendPing(byte[] message);
        [Obsolete]
        Task SendPong(byte[] message);
    }
}
