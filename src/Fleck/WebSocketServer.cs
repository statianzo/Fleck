using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Fleck
{
	public class WebSocketServer : IDisposable
	{
		private Action<IWebSocketConnection> _config;

		public WebSocketServer(string location)
		{
			var uri = new Uri(location);
			Port = uri.Port > 0 ? uri.Port : 8181;
			Location = location;
		}
		public WebSocketServer(int port, string location)
		{
			Port = port;
			Location = location;
		}

		public WebSocketServer(int port, string location, string origin)
			: this(port, location)
		{
			Origin = origin;
		}

		public ISocket ListenerSocket { get; private set; }
		public string Location { get; private set; }
		public int Port { get; private set; }
		public string Origin { get; private set; }

		public void Dispose()
		{
			((IDisposable)ListenerSocket).Dispose();
		}

		public void Start(Action<IWebSocketConnection> config)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			ListenerSocket = new SocketWrapper(socket);
			var ipLocal = new IPEndPoint(IPAddress.Any, Port);
			ListenerSocket.Bind(ipLocal);
			ListenerSocket.Listen(100);
			FleckLog.Info("Server stated on " + ipLocal);
			ListenForClients();
			_config = config;
		}

		private void ListenForClients()
		{
			Task<ISocket>.Factory.FromAsync(ListenerSocket.BeginAccept, ListenerSocket.EndAccept, null)
				.ContinueWith(OnClientConnect)
				.ContinueWith(t => FleckLog.Error("Listener socket is closed", t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private void OnClientConnect(Task<ISocket> task)
		{
			ISocket clientSocket = task.Result;
			ListenForClients();


			var shaker = new HandshakeHandler(Origin, Location)
			{
				OnSuccess = handshake =>
					{
						var wsc = new WebSocketConnection(clientSocket);
						_config(wsc);
						wsc.OnOpen();
						wsc.StartReceiving();
					}
			};

			shaker.Shake(clientSocket);

		}
	}
}