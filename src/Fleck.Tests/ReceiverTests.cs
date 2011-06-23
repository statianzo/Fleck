using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Moq;
using NUnit.Framework;

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
            _receiver = new Receiver(_mockSocket.Object,s => {}, () => {
              _wasClosed = true;
              _closeHandle.Set();
            });
        }

        [Test]
        public void ShouldNotReceiveWhenNotConnected()
        {
            _mockSocket.Setup(s => s.Connected).Returns(false);
            _receiver.Receive();
            _mockSocket.Verify(s => s.BeginReceive(It.IsAny<IList<ArraySegment<byte>>>(),It.IsAny<SocketFlags>(),It.IsAny<AsyncCallback>(),It.IsAny<object>()),Times.Never());
            Assert.IsTrue(_wasClosed);
        }

        [Test]
        public void ShouldCloseOnException()
        {
            _mockSocket.Setup(s => s.Connected).Returns(true);
            _receiver.Receive();
            _mockSocket.Setup(s => s.BeginReceive(It.IsAny<IList<ArraySegment<byte>>>(),It.IsAny<SocketFlags>(),It.IsAny<AsyncCallback>(),It.IsAny<object>()))
                .Returns<IList<ArraySegment<byte>>, SocketFlags, AsyncCallback, object>((seg, flag, call, obj) =>
                    {
                        var result = new Mock<IAsyncResult>();
                        result.Setup(r => r.AsyncState).Returns(obj);
                        result.Setup(r => r.IsCompleted).Returns(true);
                        call(result.Object);
                        return result.Object;
                    });
            _mockSocket
                .Setup(s => s.EndReceive(It.IsAny<IAsyncResult>()))
                .Throws<Exception>()
                .Verifiable();
            _receiver.Receive();
            _closeHandle.WaitOne();
            _mockSocket.Verify();
            Assert.IsTrue(_wasClosed);
        }


        
    }
}