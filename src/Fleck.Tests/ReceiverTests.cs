using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Fleck.Interfaces;

namespace Fleck.Tests
{
    [TestFixture]
    public class ReceiverTests
    {
        private bool _wasClosed;
        private Mock<ISocket> _mockSocket;
        private Receiver _receiver;
        private EventWaitHandle _closeHandle;

        [SetUp]
        public void Setup()
        {
            _wasClosed = false;
            _mockSocket = new Mock<ISocket>();
            _closeHandle = new EventWaitHandle(false,EventResetMode.ManualReset);
            _receiver = new Receiver(_mockSocket.Object);
            
            _receiver.OnError += () => {
              _wasClosed = true;
              _closeHandle.Set();
            };
        }

        [Test]
        public void ShouldNotReceiveWhenNotConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(false);
            _receiver.Receive();
            _mockSocket.Verify(s => s.Receive(It.IsAny<byte[]>(),It.IsAny<Action<int>>(),It.IsAny<Action<Exception>>(), 0), Times.Never());
            Assert.IsTrue(_wasClosed);
        }

        [Test]
        public void ShouldCloseOnException()
        {
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _receiver.Receive();
            _mockSocket.Setup(s => s.Receive(It.IsAny<byte[]>(),It.IsAny<Action<int>>(),It.IsAny<Action<Exception>>(), 0))
                .Returns<byte[], Action<int>, Action<Exception>, int>((buffer, cb, error, offset) =>
                    {
                        error(new Exception());
                        return new Task<int>(() => 0);
                    });
            _receiver.Receive();
            _closeHandle.WaitOne();
            _mockSocket.Verify();
            Assert.IsTrue(_wasClosed);
        }
    }
}