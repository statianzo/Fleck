using System;
using Moq;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class WebSocketConnectionTests
    {
        private Mock<ISocket> _socketMock;
        private WebSocketConnection _connection;
        private Mock<IHandler> _handlerMock;

        [SetUp]
        public void Setup()
        {
            _socketMock = new Mock<ISocket>();
            _handlerMock = new Mock<IHandler>();
            _connection = new WebSocketConnection(_socketMock.Object,
                                                  connection => { },
                                                  b => new WebSocketHttpRequest(),
                                                  r => _handlerMock.Object,
                                                  s => default(string));
        }

        [Test]
        public void ShouldCloseOnReadingZero()
        {
            _socketMock.SetupGet(x => x.Connected).Returns(true);
            SetupReadLengths(0);
            bool hit = false;
            _connection.OnClose = () => hit = true;
            _connection.StartReceiving();
            Assert.IsTrue(hit);
        }

        [Test]
        public void ShouldNotSendOnClosed()
        {
            _connection.Handler = _handlerMock.Object;
            SetupReadLengths(0);
            _connection.StartReceiving();
            _connection.Send("Zing");
            _socketMock.Verify(x => x.Send(It.IsAny<byte[]>(), It.IsAny<Action>(), It.IsAny<Action<Exception>>()), Times.Never());
        }

        [Test]
        public void ShouldNotSendWhenSocketDisconnected()
        {
            _connection.Handler = _handlerMock.Object;
            _socketMock.SetupGet(x => x.Connected).Returns(false);
            _connection.Send("Zing");
            _socketMock.Verify(x => x.Send(It.IsAny<byte[]>(), It.IsAny<Action>(), It.IsAny<Action<Exception>>()), Times.Never());
        }

        [Test]
        public void ShouldNotReadWhenSocketClosed()
        {
            _socketMock.SetupGet(x => x.Connected).Returns(false);
            _connection.StartReceiving();
            _socketMock.Verify(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<Action<int>>(), It.IsAny<Action<Exception>>(), 0), Times.Never());
        }

        [Test]
        public void ShouldRaiseInitializeOnFirstRead()
        {
            bool initializeRaised = false;
            var connection = new WebSocketConnection(_socketMock.Object,
                                                  conn => { initializeRaised = true; },
                                                  b => new WebSocketHttpRequest(),
                                                  r => _handlerMock.Object,
                                                  s => default(string));

            _socketMock.SetupGet(x => x.Connected).Returns(true);
            SetupReadLengths(1, 0);
            connection.StartReceiving();

            Assert.IsTrue(initializeRaised);
        }

        [Test]
        public void ShouldNotRaiseInitializeIfParseRequestReturnsNull()
        {
            bool initializeRaised = false;
            var connection = new WebSocketConnection(_socketMock.Object,
                                                  conn => { initializeRaised = true; },
                                                  b => null,
                                                  r => _handlerMock.Object,
                                                  s => default(string));

            _socketMock.SetupGet(x => x.Connected).Returns(true);
            SetupReadLengths(1, 0);
            connection.StartReceiving();

            Assert.IsFalse(initializeRaised);
        }

        [Test]
        public void ShouldNotRaiseInitializeIfHandlerFactoryReturnsNull()
        {
            bool initializeRaised = false;
            var connection = new WebSocketConnection(_socketMock.Object,
                                                  conn => { initializeRaised = true; },
                                                  b => new WebSocketHttpRequest(),
                                                  r => null,
                                                  s => null);

            _socketMock.SetupGet(x => x.Connected).Returns(true);
            SetupReadLengths(1, 0);
            connection.StartReceiving();

            Assert.IsFalse(initializeRaised);
        }

        [Test]
        public void ShouldCallOnErrorWhenError()
        {
            _socketMock.Setup(
                x =>
                x.Receive(It.IsAny<byte[]>(), It.IsAny<Action<int>>(), It.IsAny<Action<Exception>>(), It.IsAny<int>()))
                .Callback<byte[], Action<int>, Action<Exception>, int>((buffer, success, error, offset) =>
                {
                    error(new Exception());
                });

            _socketMock.SetupGet(x => x.Connected).Returns(true);

            bool hit = false;
            _connection.OnError = e => hit = true;

            _connection.StartReceiving();
            Assert.IsTrue(hit);
        }

        [Test]
        public void ShouldSwallowObjectDisposedExceptionOnRead()
        {
            _socketMock.Setup(
                x =>
                x.Receive(It.IsAny<byte[]>(), It.IsAny<Action<int>>(), It.IsAny<Action<Exception>>(), It.IsAny<int>()))
                .Callback<byte[], Action<int>, Action<Exception>, int>((buffer, success, error, offset) =>
                {
                    error(new ObjectDisposedException("socket"));
                });

            _socketMock.SetupGet(x => x.Connected).Returns(true);

            bool hit = false;
            _connection.OnError = e => hit = true;

            _connection.StartReceiving();
            Assert.IsFalse(hit);
        }

        private void SetupReadLengths(params int[] args)
        {
            var index = 0;
            _socketMock.Setup(
                x =>
                x.Receive(It.IsAny<byte[]>(), It.IsAny<Action<int>>(), It.IsAny<Action<Exception>>(), It.IsAny<int>()))
                .Callback<byte[], Action<int>, Action<Exception>, int>((buffer, success, error, offset) =>
                {
                    if (args.Length > index)
                        success(args[index++]);
                    else
                        _socketMock.SetupGet(x => x.Connected == false);
                });
        }
    }
}
