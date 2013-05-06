using System;
using System.Diagnostics;
using Moq;
using NUnit.Framework;

namespace Fleck.Wamp.Tests
{
    [TestFixture]
    public class WampSubProtocolHandlerTests
    {
        private readonly Guid _connectionId = new Guid("CFCAB391-2567-4DDE-844F-C44231CA8605");
        private const string ApplicationIdentifier = "Fleck.Wamp/0.9.6";

        private Mock<IWebSocketConnectionInfo> _webSocketConnectionInfo;
        private Mock<IWebSocketConnection> _webSocketConnection;
        private WampSubProtocolHandler _wampSubProtocolHandler;

        [SetUp]
        public void Setup()
        {
            _webSocketConnectionInfo = new Mock<IWebSocketConnectionInfo>();
            _webSocketConnectionInfo.SetupGet(x => x.Id).Returns(_connectionId);
            _webSocketConnection = new Mock<IWebSocketConnection>();
            _webSocketConnection.SetupGet(x => x.ConnectionInfo).Returns(_webSocketConnectionInfo.Object);
            _wampSubProtocolHandler = new WampSubProtocolHandler();
        }

        [Test]
        public void ShouldReturnProperIdentifier()
        {
            // Assert
            Assert.IsTrue(_wampSubProtocolHandler.Identifier.Equals("wamp"));
        }

        [Test]
        public void ShouldCallOnWelcomeWhenClientConnects()
        {
            // Arrange
            var welcomeCalled = false;
            var message = string.Empty;
            var expectedWelcomeMessage = String.Format("[0,\"{0}\",1,\"{1}\"]", _connectionId, ApplicationIdentifier);

            _webSocketConnection.SetupAllProperties();
            _webSocketConnection.Setup(x => x.Send(It.IsAny<string>())).Callback<String>(m => message = m);

            _wampSubProtocolHandler.OnWelcomeMessage += conn => { welcomeCalled = true; };
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);

            // Act
            _webSocketConnection.Object.OnOpen();

            // Assert
            Assert.IsTrue(welcomeCalled);
            Assert.IsTrue(message.Equals(expectedWelcomeMessage));
        }

        [Test]
        public void ShouldManipulateCollectionCorrectlyWhenClientConnectsOrDisconnects()
        {
            // Arrange
            _webSocketConnection.SetupAllProperties();
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);

            // Act
            _webSocketConnection.Object.OnOpen();

            // Assert
            Assert.IsTrue(_wampSubProtocolHandler.Connections.Count == 1);
            Assert.IsTrue(_wampSubProtocolHandler.Connections.ContainsKey(_connectionId));

            // Act
            _webSocketConnection.Object.OnClose();

            // Assert
            Assert.IsTrue(_wampSubProtocolHandler.Connections.Count == 0);
        }

        [Test]
        public void ShouldAddPrefixToCollection()
        {
            // Arrange
            var prefixCalled = false;
            const string intendedPrefix = "keyvalue";
            var intendedUri = new Uri("http://example.com/simple/keyvalue#");
            var prefixMessage = String.Format("[1, \"{0}\", \"{1}\"]", intendedPrefix, intendedUri);
            var returnedPrefix = String.Empty;
            Uri returnedUri = null;

            _webSocketConnection.SetupAllProperties();
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);

            _webSocketConnection.Object.OnOpen();

            _wampSubProtocolHandler.OnPrefixMessage += (conn, prefix, uri) =>
                {
                    prefixCalled = true;
                    returnedPrefix = prefix;
                    returnedUri = new Uri(uri);
                };

            // Act
            _webSocketConnection.Object.OnMessage(prefixMessage);

            // Assert
            Assert.IsTrue(prefixCalled);
            Assert.IsTrue(returnedPrefix.Equals(intendedPrefix));
            Assert.IsTrue(returnedUri.Equals(intendedUri));
            Assert.IsTrue(_wampSubProtocolHandler.Prefixes[_connectionId][intendedPrefix] == intendedUri);
        }

        [Test]
        public void ShouldHandleSubscriptionRequestProperly()
        {
            // Arrange
            var subscribeCalled = false;
            var intendedUri = new Uri("http://example.com/simple/");
            var subscriptionMessage = String.Format("[5, \"{0}\"]", intendedUri);
            Uri returnedUri = null;

            _webSocketConnection.SetupAllProperties();
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);

            _webSocketConnection.Object.OnOpen();

            _wampSubProtocolHandler.OnSubscribeMessage += (conn, uri) =>
                {
                    subscribeCalled = true;
                    returnedUri = uri;
                };
                
            // Act
            _webSocketConnection.Object.OnMessage(subscriptionMessage);

            // Assert
            Assert.IsTrue(subscribeCalled);
            Assert.IsTrue(returnedUri.Equals(intendedUri));
            Assert.IsTrue(_wampSubProtocolHandler.Subscriptions[intendedUri].Contains(_connectionId));
        }

        [Test]
        public void ShouldHandleUnsunscriptionRequestProperly()
        {
            // Arrange
            var unsubscribeCalled = false;
            var intendedUri = new Uri("http://example.com/simple/");
            var unsubscriptionMessage = String.Format("[6, \"{0}\"]", intendedUri);
            Uri returnedUri = null;

            ShouldHandleSubscriptionRequestProperly();

            _wampSubProtocolHandler.OnUnsubscribeMessage += (conn, uri) =>
            {
                unsubscribeCalled = true;
                returnedUri = uri;
            };

            // Act
            _webSocketConnection.Object.OnMessage(unsubscriptionMessage);

            // Assert
            Assert.IsTrue(unsubscribeCalled);
            Assert.IsTrue(returnedUri.Equals(intendedUri));
            Assert.IsTrue(!_wampSubProtocolHandler.Subscriptions.ContainsKey(intendedUri));
        }

        [Test]
        public void ShouldGetCustomCallbacksForMessages()
        {
            // Arrange
            const string endpoint = "http://example.com/api#storeMeal";
            var delegateCalled = false;
            const string intendedCategory = "dinner";
            var actualCategory = String.Empty;
            const int intendedCalories = 2309;
            var actualCalories = 0;
            var message = String.Format("[2, \"{0}\", \"{1}\", {{\"category\": \"{2}\", \"calories\": {3} }}]", _connectionId, endpoint, intendedCategory, intendedCalories);

            _webSocketConnection.SetupAllProperties();
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);
            _wampSubProtocolHandler.RegisterDelegateForMessage<TestCallbackMessage>(new Uri(endpoint), t =>
                {
                    delegateCalled = true;
                    actualCategory = t.Category;
                    actualCalories = t.Calories;
                });
            
            // Act
            _webSocketConnection.Object.OnOpen();
            _webSocketConnection.Object.OnMessage(message);

            // Assert
            Assert.IsTrue(delegateCalled);
            Assert.IsTrue(actualCategory.Equals(intendedCategory));
            Assert.IsTrue(actualCalories.Equals(intendedCalories));
        }
        [Test]

        public void ShouldHandlePublishRequestProperly()
        {
            // Arrange
            var eventCalled = false;
            var publishCalled = false;
            var intendedUri = new Uri("http://example.com/simple/");
            var subscriptionMessage = String.Format("[5, \"{0}\"]", intendedUri);
            Uri eventMessageReturnedUri = null;
            Uri publishMessageReturnedUri = null;
            const string intendedEvent = "Hello, world!";
            var message = String.Format("[7, \"{0}\", \"{1}\"]", intendedUri, intendedEvent);
            var eventMessageReturnedEvent = String.Empty;
            var publishMessageReturnedEvent = String.Empty;

            _webSocketConnection.SetupAllProperties();
            _wampSubProtocolHandler.SubProtocolInitializer(_webSocketConnection.Object);

            _webSocketConnection.Object.OnOpen();
            _webSocketConnection.Object.OnMessage(subscriptionMessage);

            _wampSubProtocolHandler.OnEventMessage += (connection, uri, eventId) => 
            {
                eventCalled = true;
                eventMessageReturnedUri = uri;
                eventMessageReturnedEvent = eventId;
            };

            _wampSubProtocolHandler.OnPublishMessage += (connection, uri, eventId, excludeList, eligibleList) =>
            {
                publishCalled = true;
                publishMessageReturnedUri = uri;
                publishMessageReturnedEvent = eventId;
            };

            // Act
            _webSocketConnection.Object.OnMessage(message);

            // Assert
            Assert.IsTrue(eventCalled);
            Assert.IsTrue(eventMessageReturnedUri.Equals(intendedUri));
            Assert.IsTrue(intendedEvent.Equals(eventMessageReturnedEvent));
            Assert.IsTrue(publishCalled);
            Assert.IsTrue(publishMessageReturnedUri.Equals(intendedUri));
            Assert.IsTrue(intendedEvent.Equals(publishMessageReturnedEvent));
            Assert.IsTrue(_wampSubProtocolHandler.Subscriptions[intendedUri].Contains(_connectionId));
        }

    }

    public class TestCallbackMessage
    {
        public string Category { get; set; }
        public int Calories { get; set; }
    }
}
