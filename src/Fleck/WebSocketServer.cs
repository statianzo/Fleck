using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Fleck
{
	public class WebSocketServer : IDisposable
	{
		private Action<WebSocketConnection> _config;

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

		public Socket ListenerSocket { get; private set; }
		public string Location { get; private set; }
		public int Port { get; private set; }
		public string Origin { get; private set; }

		public void Dispose()
		{
			((IDisposable)ListenerSocket).Dispose();
		}

		public void Start(Action<WebSocketConnection> config)
		{
			ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			var ipLocal = new IPEndPoint(IPAddress.Any, Port);
			ListenerSocket.Bind(ipLocal);
			ListenerSocket.Listen(100);
			Log.Info("Server stated on " + ListenerSocket.LocalEndPoint);
			ListenForClients();
			_config = config;
		}

		private void ListenForClients()
		{
			Task<Socket>.Factory.FromAsync(ListenerSocket.BeginAccept, ListenerSocket.EndAccept, null)
				.ContinueWith(OnClientConnect)
				.ContinueWith(t => Log.Error("Listener socket is closed"), TaskContinuationOptions.OnlyOnFaulted);
		}

		private void OnClientConnect(Task<Socket> task)
		{
			Socket clientSocket = task.Result;
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