using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Moq;
using NUnit.Framework;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.Tests
{
    [TestFixture]
    public class WebSocketServerTests
    {
        private WebSocketServer _server;
        private MockRepository _repository;

        private IPAddress _ipV4Address;
        private IPAddress _ipV6Address;

        private Socket _ipV4Socket;
        private Socket _ipV6Socket;

        [SetUp]
        public void Setup()
        {
            _repository = new MockRepository(MockBehavior.Default);
            _server = new WebSocketServer("ws://0.0.0.0:8000");

            _ipV4Address = IPAddress.Parse("127.0.0.1");
            _ipV6Address = IPAddress.Parse("::1");

            _ipV4Socket = new Socket(_ipV4Address.AddressFamily, SocketType.Stream, ProtocolType.IP);
            _ipV6Socket = new Socket(_ipV6Address.AddressFamily, SocketType.Stream, ProtocolType.IP);
        }

        [Test]
        public void ShouldStart()
        {
            var socketMock = _repository.Create<ISocket>();

            _server.ListenerSocket = socketMock.Object;
            _server.Start(connection => { });

            socketMock.Verify(s => s.Bind(It.Is<IPEndPoint>(i => i.Port == 8000)));
            socketMock.Verify(s => s.Accept(It.IsAny<Action<ISocket>>(), It.IsAny<Action<Exception>>()));
        }

        [Test]
        public void ShouldFailToParseIPAddressOfLocation()
        {
            Assert.Throws(typeof(FormatException), () => {
                new WebSocketServer("ws://localhost:8000");
            });
        }

        [Test]
        public void ShouldBeSecureWithWssAndCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            server.Certificate = new X509Certificate2();
            Assert.IsTrue(server.IsSecure);
        }

        [Test]
        public void ShouldDefaultToNoneWithWssAndCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            server.Certificate = new X509Certificate2();
            Assert.AreEqual(server.EnabledSslProtocols, SslProtocols.None);
        }

        [Test]
        public void ShouldNotBeSecureWithWssAndNoCertificate()
        {
            var server = new WebSocketServer("wss://0.0.0.0:8000");
            Assert.IsFalse(server.IsSecure);
        }

        [Test]
        public void ShouldNotBeSecureWithoutWssAndCertificate()
        {
            var server = new WebSocketServer("ws://0.0.0.0:8000");
            server.Certificate = new X509Certificate2();
            Assert.IsFalse(server.IsSecure);
        }

        [Test]
        public void ShouldSupportDualStackListenWhenServerV4All()
        {
            _server = new WebSocketServer("ws://0.0.0.0:8000");
            _server.Start(connection => { });
            _ipV4Socket.Connect(_ipV4Address, 8000);
            _ipV6Socket.Connect(_ipV6Address, 8000);
        }

        #if __MonoCS__
          // None
        #else

            [Test]
            public void ShouldSupportDualStackListenWhenServerV6All()
            {
                _server = new WebSocketServer("ws://[::]:8000");
                _server.Start(connection => { });
                _ipV4Socket.Connect(_ipV4Address, 8000);
                _ipV6Socket.Connect(_ipV6Address, 8000);
            }

        #endif

        [TearDown]
        public void TearDown()
        {
            _ipV4Socket.Dispose();
            _ipV6Socket.Dispose();
            _server.Dispose();
        }
    }
}
