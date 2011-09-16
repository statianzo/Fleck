using System.Text;
using Fleck.ResponseBuilders;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class Hybi14ResponseBuilderTests
    {
        private Hybi14ResponseBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _builder = new Hybi14ResponseBuilder();
        }

        [Test]
        public void ShouldHandleSocketVersion13And8()
        {
            var request = new WebSocketHttpRequest {
                                  Headers = {{"Sec-WebSocket-Version", "13"}}
                              };

            Assert.True(_builder.CanHandle(request));

            request = new WebSocketHttpRequest {
                                  Headers = {{"Sec-WebSocket-Version", "8"}}
                              };

            Assert.True(_builder.CanHandle(request));

        }

        [Test]
        public void ShouldCreateAnswerGuid()
        {
            const string exampleRequestKey = "dGhlIHNhbXBsZSBub25jZQ==";
            const string expectedResult = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

            var actual = _builder.CreateResponseKey(exampleRequestKey);

            Assert.AreEqual(expectedResult, actual);
        }

        [Test]
        public void ShouldRespondToCompleteRequestCorrectly()
        {
            var request = new WebSocketHttpRequest
                              {
                                  Method = "GET",
                                  Path = "/chat",
                                  Body = "",
                                  Headers =
                                      {
                                          {"Host", "server.example.com"},
                                          {"Upgrade", "websocket"},
                                          {"Connection", "Upgrade"},
                                          {"Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ=="},
                                          {"Origin", "http://example.com"},
                                          {"Sec-WebSocket-Protocol", "chat, superchat"},
                                          {"Sec-WebSocket-Version", "13"}
                                      },
                                  Bytes = Encoding.ASCII.GetBytes(ExampleRequest)
                              };

            var result = _builder.Build(request);

            Assert.AreEqual(ExampleResponse, Encoding.ASCII.GetString(result));
        }

        private const string ExampleRequest =
@"GET /chat HTTP/1.1
Host: server.example.com
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
Origin: http://example.com
Sec-WebSocket-Protocol: chat, superchat
Sec-WebSocket-Version: 13

";

        private const string ExampleResponse =
@"HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=

";
    }
}