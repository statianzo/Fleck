using System;
using System.Net.Sockets;

namespace Nugget
{
	public class WebSocketConnection
	{
		public WebSocketConnection(Socket socket)
		{
			Socket = socket;
			Sender = new Sender(this);
			Receiver = new Receiver(this);
			OnOpen = () => { };
			OnClose = () => { };
			OnMessage = x => { };
		}

		public Socket Socket { get; set; }


		private Sender Sender { get; set; }
		private Receiver Receiver { get; set; }

		public Action OnOpen { get; set; }
		public Action OnClose { get; set; }
		public Action<string> OnMessage { get; set; }

		public void Send(string data)
		{
			Sender.Send(data);
		}

		public void StartReceiving()
		{
			Receiver.Receive();
		}
	}
}