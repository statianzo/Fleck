using System;

namespace Fleck
{
	public class WebSocketConnection : IWebSocketConnection
	{
		public WebSocketConnection(ISocket socket)
		{
			Socket = socket;
			_sender = new Sender(socket, Close);
			_receiver = new Receiver(socket, s => OnMessage(s), Close);
			OnOpen = () => { };
			OnClose = () => { };
			OnMessage = x => { };
		}

		public ISocket Socket { get; set; }


		private readonly Sender _sender;
		private readonly Receiver _receiver;

		public Action OnOpen { get; set; }
		public Action OnClose { get; set; }
		public Action<string> OnMessage { get; set; }

		public void Send(string message)
		{
			_sender.Send(message);
		}

		public void StartReceiving()
		{
			_receiver.Receive();
		}

		public void Close()
		{
			OnClose();
			Socket.Close();
		}
	}

}