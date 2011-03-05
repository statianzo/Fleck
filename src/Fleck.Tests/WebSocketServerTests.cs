using System;
using System.Net;
using Moq;
using NUnit.Framework;

namespace Fleck.Tests
{
	[TestFixture]
	public class WebSocketServerTests
	{
		private WebSocketServer _server;
		private MockRepository _repository;

		[SetUp]
		public void Setup()
		{
			_repository = new MockRepository(MockBehavior.Default);
			_server = new WebSocketServer("ws://localhost:8000");
		}

		[Test]
		public void ShouldStart()
		{
			var socketMock = _repository.Create<ISocket>();

			_server.ListenerSocket = socketMock.Object;
			_server.Start(connection => { });

			socketMock.Verify(s => s.Bind(It.Is<IPEndPoint>(i => i.Port == 8000)));
			socketMock.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), It.IsAny<object>()));
		}
	}
}