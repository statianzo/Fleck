using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Fleck.Interfaces;
using Fleck.ResponseBuilders;

namespace Fleck
{
    public class WebSocketServer : IDisposable
    {
        private readonly string _scheme;
        private Action<IWebSocketConnection> _config;
        private X509Certificate2 _x509Certificate;

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
            ResponseBuilderFactory = new ResponseBuilderFactory();
        }
        
        private void RegisterResponseBuilders()
        {
            ResponseBuilderFactory.Register(new Draft76ResponseBuilder(Location, _scheme, Origin));
        }

        public ISocket ListenerSocket { get; set; }
        public string Location { get; private set; }
        public int Port { get; private set; }
        public string Origin { get; set; }
        public string Certificate { get; set; }
        public IResponseBuilderFactory ResponseBuilderFactory { get; set; }

        public bool IsSecure
        {
            get { return _scheme == "wss" && Certificate != null; }
        }

        public void Dispose()
        {
            ListenerSocket.Close();
        }

        public void Start(Action<IWebSocketConnection> config)
        {
            var ipLocal = new IPEndPoint(IPAddress.Any, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);
            FleckLog.Info("Server started at " + Location);
            if (_scheme == "wss")
            {
                if (Certificate == null)
                {
                    FleckLog.Error("Scheme cannot be 'wss' without a Certificate");
                    return;
                }
                _x509Certificate = new X509Certificate2(Certificate);
            }
            RegisterResponseBuilders();
            ListenForClients();
            _config = config;
        }

        private void ListenForClients()
        {
            ListenerSocket.Accept(OnClientConnect, e => FleckLog.Error("Listener socket is closed", e));
        }

        private void OnClientConnect(ISocket clientSocket)
        {
            FleckLog.Debug("Client Connected");
            ListenForClients();

            var shaker = new HandshakeHandler(ResponseBuilderFactory)
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

            if (IsSecure)
            {
                FleckLog.Debug("Authenticating Secure Connection");
                clientSocket
                    .Authenticate(_x509Certificate,
                                  () =>
                                  {
                                      FleckLog.Debug("Authentication Successful");
                                      shaker.Shake(clientSocket);
                                  }, e => FleckLog.Warn("Failed to Authenticate", e));
            }
            else
            {
                shaker.Shake(clientSocket);
            }
        }
    }
}