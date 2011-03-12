using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class SenderTests
    {
        private bool _wasHit;
        private Mock<ISocket> _mockSocket;
        private Sender _sender;

        [SetUp]
        public void Setup()
        {
            _wasHit = false;
            _mockSocket = new Mock<ISocket>();
            _sender = new Sender(_mockSocket.Object, () => _wasHit = true);
        }

        [Test]
        public void DoesNotSendWhenSocketNotConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(false);
            _sender.Send("Data!");
            _mockSocket.Verify(s => s.BeginSend(It.IsAny<IList<ArraySegment<byte>>>(),It.IsAny<SocketFlags>(),It.IsAny<AsyncCallback>(),It.IsAny<object>()),Times.Never());
            Assert.IsFalse(_wasHit);
        }

        [Test]
        public void SendsWhenSocketConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _sender.Send("Data!");
            _mockSocket.Verify(s => s.BeginSend(It.IsAny<IList<ArraySegment<byte>>>(),It.IsAny<SocketFlags>(),It.IsAny<AsyncCallback>(),It.IsAny<object>()),Times.Once());
            Assert.IsFalse(_wasHit);
        }
        
        [Test]
        public void CallsCloseActionOnException()
        {
            _mockSocket.Setup(
                s =>
                s.BeginSend(It.IsAny<IList<ArraySegment<byte>>>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(),
                            It.IsAny<object>()))
                .Returns<IList<ArraySegment<byte>>, SocketFlags, AsyncCallback, object>((seg, flag, call, obj) =>
                    {
                        var result = new Mock<IAsyncResult>();
                        result.Setup(r => r.AsyncState).Returns(obj);
                        call(result.Object);
                        return result.Object;
                    });
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _mockSocket
                .Setup(s => s.EndSend(It.IsAny<IAsyncResult>()))
                .Throws<Exception>()
                .Verifiable();
            _sender.Send("Data!");
            Thread.Sleep(100);
            _mockSocket.Verify();
            Assert.IsTrue(_wasHit);
        }
    }
}