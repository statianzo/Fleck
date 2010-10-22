using System;
using System.Net.Sockets;
using System.Net;

namespace Fleck
{
	public class WebSocketServer : IDisposable
	{
		private Action<WebSocketConnection> _config;

		public WebSocketServer(int port, string origin, string location)
		{
			Port = port;
			Origin = origin;
			Location = location;
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
			ListenerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
		}

		private void OnClientConnect(IAsyncResult ar)
		{
			Socket clientSocket;

			try
			{
				clientSocket = ListenerSocket.EndAccept(ar);
			}
			catch
			{
				Log.Error("Listener socket is closed");
				return;
			}


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

			ListenForClients();
		}
	}
}