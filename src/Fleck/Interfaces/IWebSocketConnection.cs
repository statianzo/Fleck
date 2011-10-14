using System;

namespace Fleck
{
    public interface IWebSocketConnection
    {
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }
        Action<Exception> OnError { get; set; }
        void Send(string message);
        void Close();
        IWebSocketConnectionInfo ConnectionInfo { get; }
    }
}
