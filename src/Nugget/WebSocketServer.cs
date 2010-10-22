using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace Nugget
{
	public class WebSocketServer : IDisposable
	{
		private readonly List<WebSocketConnection> Connections = new List<WebSocketConnection>();
		private readonly SubProtocolModelFactoryStore ModelFactories = new SubProtocolModelFactoryStore();
		private readonly WebSocketFactory SocketFactory = new WebSocketFactory();

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
			ListenerSocket.Dispose();
		}

		public void RegisterHandler<TSocket>(string path) where TSocket : IWebSocket
		{
			SocketFactory.Register<TSocket>(path);
		}

		public void RegisterHandler(Type handler, string path)
		{
			SocketFactory.Register(handler, path);
		}

		public void SetSubProtocolModelFactory<TModel>(ISubProtocolModelFactory<TModel> factory, string subprotocol)
		{
			ModelFactories.Store(factory, subprotocol);
		}

		public void Start()
		{
			ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			var ipLocal = new IPEndPoint(IPAddress.Any, Port);
			ListenerSocket.Bind(ipLocal);
			ListenerSocket.Listen(100);
			Log.Info("Server stated on " + ListenerSocket.LocalEndPoint);
			ListenForClients();
		}

		private void ListenForClients()
		{
			ListenerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
		}

		private void OnClientConnect(IAsyncResult ar)
		{
			Socket clientSocket = null;

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
						WebSocketConnection wsc = SocketFactory.Create(handshake.ResourcePath);
						if (wsc != null)
						{
							wsc.Socket = clientSocket;

							if (handshake.SubProtocol != null)
							{
								wsc.SetModelFactory(ModelFactories.Get(handshake.SubProtocol));
							}

							wsc.WebSocket.Connected(handshake);

							wsc.StartReceiving();

							Connections.Add(wsc);
						}
					}
			};

			shaker.Shake(clientSocket);

			ListenForClients();
		}

		public void SendToAll(string message)
		{
			foreach (WebSocketConnection c in Connections)
			{
				c.Send(message);
			}
		}
	}
}