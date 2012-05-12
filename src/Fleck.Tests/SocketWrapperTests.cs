using System;
using NUnit.Framework;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace Fleck.Tests
{
    [TestFixture]
    public class SocketWrapperTests
    {
        private Socket _socket;
        private Socket _listener;
        private Socket _client;
        private EndPoint _endpoint;
        private SocketWrapper _wrapper;

        [SetUp]
        public void Setup()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _endpoint = new IPEndPoint(IPAddress.Loopback, 45982);
            _listener.Bind(_endpoint);
            _listener.Listen(10);

            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            ThreadPool.QueueUserWorkItem(x => {
                Thread.Sleep(100);
                _client.Connect(_endpoint);
            });
            _socket = _listener.Accept();

            _wrapper = new SocketWrapper(_socket);
        }

        [TearDown]
        public void TearDown()
        {
            _socket.Dispose();
            _client.Dispose();
            _listener.Dispose();
            _wrapper.Dispose();
        }

        [Test]
        public void ShouldHandleObjectDisposedOnSend()
        {
            Exception ex = null;
            _wrapper.Dispose();
            _wrapper.Send(new byte[1], () => {}, e => {ex = e;});
            Assert.IsInstanceOf<ObjectDisposedException>(ex);
        }
        [Test]
        public void ShouldHandleObjectDisposedOnReceive()
        {
            Exception ex = null;
            _wrapper.Dispose();
            _wrapper.Receive(new byte[1], i => {}, e => {ex = e;}, 0);
            Assert.IsInstanceOf<ObjectDisposedException>(ex);
        }
    }
}

