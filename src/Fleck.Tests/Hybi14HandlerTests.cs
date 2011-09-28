using System.Text;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class Hybi14HandlerTests
    {

        [Test]
        public void ShouldCreateAnswerGuid()
        {
            const string exampleRequestKey = "dGhlIHNhbXBsZSBub25jZQ==";
            const string expectedResult = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

            var actual = Hybi13Handler.CreateResponseKey(exampleRequestKey);

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

            var result = Hybi13Handler.BuildHandshake(request);

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