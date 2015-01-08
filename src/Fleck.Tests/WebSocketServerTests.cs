using System;
using System.Net;
using System.Net.Sockets;
using Moq;
using NUnit.Framework;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.Tests
{
    [TestFixture]
    public class WebSocketServerTests
    {
        private WebSocketServer _server;
        private Socket _socket;
        private MockRepository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = new MockRepository(MockBehavior.Default);
            _server = new WebSocketServer("ws://0.0.0.0:8000");
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
        public void ShouldSupportDualStackAllInterfaces()
        {
            _server.Start(connection => { });
            var v6Address = IPAddress.Parse("::1");
            _socket = new Socket(v6Address.AddressFamily, SocketType.Stream, ProtocolType.IP);
            _socket.Connect(v6Address, 8000);
        }

        [TearDown]
        public void TearDown()
        {
            _socket.Dispose();
            _server.Dispose();
        }
    }
}
