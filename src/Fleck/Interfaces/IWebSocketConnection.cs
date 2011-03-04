using System;

namespace Fleck
{
	public interface IWebSocketConnection
	{
		Action OnOpen { get; set; }
		Action OnClose { get; set; }
		Action<string> OnMessage { get; set; }
		void Send(string message);
	}
}