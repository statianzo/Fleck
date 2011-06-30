using System;
using System.Threading;
using System.Threading.Tasks;
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
        private EventWaitHandle _closeHandle;

        [SetUp]
        public void Setup()
        {
            _wasHit = false;
            _mockSocket = new Mock<ISocket>();
            _closeHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            _sender = new Sender(_mockSocket.Object, () => 
            {
              _wasHit = true;
              _closeHandle.Set();
            });
        }

        [Test]
        public void DoesNotSendWhenSocketNotConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(false);
            _sender.Send("Data!");
            _mockSocket.Verify(s => s.Send(It.IsAny<byte[]>(), It.IsAny<Action>(), It.IsAny<Action<Exception>>()),Times.Never());
            Assert.IsFalse(_wasHit);
        }

        [Test]
        public void SendsWhenSocketConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _sender.Send("Data!");
            _mockSocket.Verify(s => s.Send(It.IsAny<byte[]>(), It.IsAny<Action>(), It.IsAny<Action<Exception>>()),Times.Once());
            Assert.IsFalse(_wasHit);
        }
        
        [Test]
        public void CallsCloseActionOnException()
        {
            _mockSocket.Setup(
                s =>
                s.Send(It.IsAny<byte[]>(), It.IsAny<Action>(), It.IsAny<Action<Exception>>()))
                .Returns<byte[], Action, Action<Exception>>((buffer, cb, error) =>
                    {
                        error(new Exception());
                        return new Task(() => { });
                    });
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _sender.Send("Data!");
            _closeHandle.WaitOne();
            _mockSocket.Verify();
            Assert.IsTrue(_wasHit);
        }
    }
}