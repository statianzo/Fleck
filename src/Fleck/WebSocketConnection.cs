using System;
using System.Net.Sockets;

namespace Fleck
{
	public class WebSocketConnection
	{
		public WebSocketConnection(Socket socket)
		{
			Socket = socket;
			_sender = new Sender(this);
			_receiver = new Receiver(this);
			OnOpen = () => { };
			OnClose = () => { };
			OnMessage = x => { };
		}

		public Socket Socket { get; set; }


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