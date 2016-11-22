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
    public class WebSocketRelayTests
    {
        private WebSocketRelay _relay;

        [SetUp]
        public void Setup()
        {
            this._relay = new WebSocketRelay();
        }

        [Test]
        public void ConnectionsShouldNotExceedMaxLength()
        {
            this._relay = new WebSocketRelay(Guid.NewGuid(), "My Relay", 5);
            for (int i = 0; i < 10; i++)
            {
                this._relay.Add(new WebSocketConnection(null, null, null, null, null));
                if (i >= 5) Assert.That(this._relay.Count == 5);
            }
        }
    }
}
