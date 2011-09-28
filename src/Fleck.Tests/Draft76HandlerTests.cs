using System;
using NUnit.Framework;
using System.Text;
using System.Linq;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class Draft76HandlerTests
    {
        
        private const string ExampleRequest =
@"GET /demo HTTP/1.1
Host: example.com
Connection: Upgrade
Sec-WebSocket-Key2: 12998 5 Y3 1  .P00
Sec-WebSocket-Protocol: sample
Upgrade: WebSocket
Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5
Origin: http://example.com

^n:ds[4U";

        private const string ExampleResponse =
@"HTTP/1.1 101 WebSocket Protocol Handshake
Upgrade: WebSocket
Connection: Upgrade
Sec-WebSocket-Origin: http://example.com
Sec-WebSocket-Location: ws://example.com/demo
Sec-WebSocket-Protocol: sample

8jKS'y:G*Co,Wxa-";

        const string Key1 = "4 @1  46546xW%0l 1 5";
        const string Key2 = "12998 5 Y3 1  .P00";
        const string Challenge = "^n:ds[4U";
        const string ExpectedAnswer = "8jKS'y:G*Co,Wxa-";
        
        [Test]
        public void ShouldGenerateServerHandshake()
        {
            var request = new WebSocketHttpRequest
            {
                Headers = {
                    {"Sec-WebSocket-Key1",Key1},
                    {"Sec-WebSocket-Key2",Key2},
                    {"Host","example.com"},
                    {"Connection","Upgrade"},
                    {"Sec-WebSocket-Protocol", "sample"},
                    {"Origin","http://example.com"},
                },
                Body = Challenge,
                Scheme = "ws",
                Path = "/demo",
                Bytes = Encoding.UTF8.GetBytes(ExampleRequest)
            };
            var responseBytes = Draft76Handler.Handshake(request);
            var response = Encoding.ASCII.GetString(responseBytes);

            Assert.AreEqual(ExampleResponse, response);

        }

        [Test]
        public void ShouldCalculateAnswerBytes()
        {
            var challengeBytes = Encoding.UTF8.GetBytes(Challenge);
            var challengeSegment = new ArraySegment<byte>(challengeBytes);
            var answerBytes = Draft76Handler.CalculateAnswerBytes(Key1, Key2, challengeSegment);

            Assert.AreEqual(16, answerBytes.Length);

            var answer = Encoding.UTF8.GetString(answerBytes);

            Assert.AreEqual(ExpectedAnswer, answer);
        }
    }
}

