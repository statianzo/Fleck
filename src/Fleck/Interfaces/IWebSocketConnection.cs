using System;

namespace Fleck
{
    public interface IWebSocketConnection
    {
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }
        Action<byte[]> OnBinary { get; set; }
        Action<Exception> OnError { get; set; }
        void Send(string message);
        void Send(byte[] message);
        void Close();
        IWebSocketConnectionInfo ConnectionInfo { get; }
    }
}
