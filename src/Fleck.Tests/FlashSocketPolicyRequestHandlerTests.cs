using System;
using Fleck.Handlers;
using System.Text;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class FlashSocketPolicyRequestHandlerTests
    {

        private IHandler _handler;
        private WebSocketHttpRequest _request;

        [SetUp]
        public void Setup()
        {
            _request = new WebSocketHttpRequest();
            _handler = FlashSocketPolicyRequestHandler.Create(_request);
        }

        [Test]
        public void ShouldGeneratePolicyResponse()
        {
            _request.Bytes = Encoding.UTF8.GetBytes("<policy-file-request />\0");

            var responseBytes = _handler.CreateHandshake();

            var response = Encoding.ASCII.GetString(responseBytes);

            Assert.AreEqual(FlashSocketPolicyRequestHandler.PolicyResponse, response);
        }
    }
}

