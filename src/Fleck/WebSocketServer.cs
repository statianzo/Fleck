using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Fleck
{
    public class WebSocketServer : IWebSocketServer
    {
        private readonly string _scheme;
        private Action<IWebSocketConnection> _config;
        private X509Certificate2 _x509Certificate;
        private bool _disposed;

        public WebSocketServer(string location)
            : this(8181, location)
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
        public string Certificate { get; set; }

        public bool IsSecure
        {
            get { return _scheme == "wss" && Certificate != null; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WebSocketServer()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                ListenerSocket.Dispose();
                ListenerSocket = null;
            }

            _disposed = true;
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
            ListenForClients();
            _config = config;
        }

        private void ListenForClients()
        {
            if (ListenerSocket == null) return;
            
            ListenerSocket.Accept(OnClientConnect, e => FleckLog.Error("Listener socket is closed", e));
        }

        private void OnClientConnect(ISocket clientSocket)
        {
            FleckLog.Debug("Client Connected");
            ListenForClients();

            WebSocketConnection connection = null;

            connection = new WebSocketConnection(
                clientSocket,
                bytes => RequestParser.Parse(bytes, _scheme),
                r => HandlerFactory.BuildHandler(r, s => connection.OnMessage(s),
                                                         connection.Close));

            _config(connection);


            if (IsSecure)
            {
                FleckLog.Debug("Authenticating Secure Connection");
                clientSocket
                    .Authenticate(_x509Certificate,
                                  connection.StartReceiving,
                                  e => FleckLog.Warn("Failed to Authenticate", e));
            }
            else
            {
                connection.StartReceiving();
            }
        }
    }
}
