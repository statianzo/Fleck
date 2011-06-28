using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Fleck
{
	public class WebSocketServer : IDisposable
	{
		private Action<IWebSocketConnection> _config;
    private readonly string _scheme;
    private X509Certificate2 _x509certificate;

		public WebSocketServer(string location) : this(8181, location)
		{
		}
		public WebSocketServer(int port, string location)
		{
      var uri = new Uri(location); 
			Port = uri.Port > 0 ? uri.Port : port;
			Location = location;
      _scheme = uri.Scheme;    
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			ListenerSocket = new SocketWrapper(socket);
		}

		public ISocket ListenerSocket { get; set; }
		public string Location { get; private set; }
		public int Port { get; private set; }
		public string Origin { get; set; }
    public string Certificate { get; set; } 
    public bool IsSecure { get { return _scheme == "wss" && Certificate != null; }} 

		public void Dispose()
		{
			((IDisposable)ListenerSocket).Dispose();
		}
  
		public void Start(Action<IWebSocketConnection> config)
		{
			var ipLocal = new IPEndPoint(IPAddress.Any, Port);
			ListenerSocket.Bind(ipLocal);
			ListenerSocket.Listen(100);
			FleckLog.Info("Server started at " + Location);
      if (_scheme == "wss") {
        if (Certificate == null) {
          FleckLog.Error("Scheme cannot be 'wss' without a Certificate");
          return;
        }
        _x509certificate = new X509Certificate2(Certificate);
      }
			ListenForClients();
			_config = config;
		}

		private void ListenForClients()
		{
			
			var task = Task.Factory.FromAsync<ISocket>(ListenerSocket.BeginAccept, ListenerSocket.EndAccept, null);
			task.ContinueWith(OnClientConnect, TaskContinuationOptions.NotOnFaulted);
			task.ContinueWith(t => FleckLog.Error("Listener socket is closed", t.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		private void OnClientConnect(Task<ISocket> task)
		{
			FleckLog.Debug("Client Connected");
			ISocket clientSocket = task.Result;
      ListenForClients();  

			var shaker = new HandshakeHandler(Origin, Location, _scheme)
			{
				OnSuccess = handshake =>
					{
						FleckLog.Debug("Handshake success");
						var wsc = new WebSocketConnection(clientSocket);
						_config(wsc);
						wsc.OnOpen();
						wsc.StartReceiving();
					}
			};
     
      if (IsSecure) {
        FleckLog.Debug("Authenticating Secure Connection");
        clientSocket.Authenticate(_x509certificate, () => {
          FleckLog.Debug("Authentication Successful");
          shaker.Shake(clientSocket);
        },e => FleckLog.Warn("Failed to Authenticate", e) );
      }  
      else {
        shaker.Shake(clientSocket);
      }

		}
	}
}