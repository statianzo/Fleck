using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Security.Authentication;
using Fleck.Helpers;

namespace Fleck
{
    public class WebSocketServer : IWebSocketServer
    {
        private readonly string _scheme;
        private readonly IPAddress _locationIP;
        private Action<IWebSocketConnection> _config;

        public WebSocketServer(string location)
            : this(8181, location)
        {
        }

        public WebSocketServer(int port, string location)
        {
            var uri = new Uri(location);
            Port = uri.Port > 0 ? uri.Port : port;
            Location = location;
            _locationIP = ParseIPAddress(uri);
            _scheme = uri.Scheme;
            var socket = new Socket(_locationIP.AddressFamily, SocketType.Stream, ProtocolType.IP);
            if(!MonoHelper.IsRunningOnMono()){
                  #if __MonoCS__
                  #else
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                  #endif
            }
            ListenerSocket = new SocketWrapper(socket);
            SupportedSubProtocols = new string[0];
        }

        public ISocket ListenerSocket { get; set; }
        public string Location { get; private set; }
        public int Port { get; private set; }
        public X509Certificate2 Certificate { get; set; }
        public SslProtocols EnabledSslProtocols { get; set; }
        public IEnumerable<string> SupportedSubProtocols { get; set; }

        public bool IsSecure
        {
            get { return _scheme == "wss" && Certificate != null; }
        }

        public void Dispose()
        {
            ListenerSocket.Dispose();
        }

        private IPAddress ParseIPAddress(Uri uri)
        {
            string ipStr = uri.Host;

            if (ipStr == "0.0.0.0" || ipStr == "[0000:0000:0000:0000:0000:0000:0000:0000]")
            {
                return IPAddress.IPv6Any;
            } else {
                try {
                    return IPAddress.Parse(ipStr);
                } catch (Exception ex) {
                    throw new FormatException("Failed to parse the IP address part of the location. Please make sure you specify a valid IP address. Use 0.0.0.0 or [::] to listen on all interfaces.", ex);
                }
            }
        }

        public void Start(Action<IWebSocketConnection> config)
        {
            var ipLocal = new IPEndPoint(_locationIP, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);
            Port = ((IPEndPoint)ListenerSocket.LocalEndPoint).Port;
            FleckLog.Info(string.Format("Server started at {0} (actual port {1})", Location, Port));
            if (_scheme == "wss")
            {
                if (Certificate == null)
                {
                    FleckLog.Error("Scheme cannot be 'wss' without a Certificate");
                    return;
                }

                if (EnabledSslProtocols == SslProtocols.None)
                {
                    EnabledSslProtocols = SslProtocols.Tls;
                    FleckLog.Debug("Using default TLS 1.0 security protocol.");
                }
            }
            ListenForClients();
            _config = config;
        }

        private void ListenForClients()
        {
            ListenerSocket.Accept(OnClientConnect, e => FleckLog.Error("Listener socket is closed", e));
        }

        private void OnClientConnect(ISocket clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            FleckLog.Debug(String.Format("Client connected from {0}:{1}", clientSocket.RemoteIpAddress, clientSocket.RemotePort.ToString()));
            ListenForClients();

            WebSocketConnection connection = null;

            connection = new WebSocketConnection(
                clientSocket,
                _config,
                bytes => RequestParser.Parse(bytes, _scheme),
                r => HandlerFactory.BuildHandler(r,
                                                 s => connection.OnMessage(s),
                                                 connection.Close,
                                                 b => connection.OnBinary(b),
                                                 b => connection.OnPing(b),
                                                 b => connection.OnPong(b)),
                s => SubProtocolNegotiator.Negotiate(SupportedSubProtocols, s));

            if (IsSecure)
            {
                FleckLog.Debug("Authenticating Secure Connection");
                clientSocket
                    .Authenticate(Certificate,
                                  EnabledSslProtocols,
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
