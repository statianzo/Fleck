using System;
using NUnit.Framework;
using System.Text;
using Fleck.RequestBuilders;
using System.Linq;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class Draft76ResponseBuilderTests
    {
        Draft76ResponseBuilder _builder;
        public Draft76ResponseBuilderTests()
        {
            _builder = new Draft76ResponseBuilder("example.com/demo", "ws", null);
        }
        
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
        public void ShouldParseClientHandshake()
        {
            var request = new WebSocketHttpRequest
            {
                Headers = {
                    {"Sec-WebSocket-Key1",Key1},
                    {"Sec-WebSocket-Key2",Key2},
                    {"Host","example.com"},
                    {"Connection","Upgrade"},
                    {"Origin","http://example.com"},
                },
                Body = Challenge,
                Bytes = Encoding.UTF8.GetBytes(ExampleRequest)
            };
            var client = Draft76ResponseBuilder.ParseClientHandshake(request);

            Assert.AreEqual(Key1, client.Key1);
            Assert.AreEqual(Key2, client.Key2);
            Assert.AreEqual(request.Headers["Origin"], client.Origin);
            Assert.AreEqual(request.Headers["Host"], client.Host);
            var clientChallenge = client.ChallengeBytes.Array.Skip(client.ChallengeBytes.Offset).Take(client.ChallengeBytes.Count).ToArray();
            var clientChallengeString = Encoding.UTF8.GetString(clientChallenge);
            Assert.AreEqual(Challenge, clientChallengeString);
        }

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
                Path = "/demo",
                Bytes = Encoding.UTF8.GetBytes(ExampleRequest)
            };
            var client = Draft76ResponseBuilder.ParseClientHandshake(request);
            var server = _builder.GenerateResponseHandshake(client);

            Assert.IsTrue(server.Location.Contains(client.Host));
            var answer = Encoding.UTF8.GetString(server.AnswerBytes);
            Assert.AreEqual(ExpectedAnswer,answer);

            Assert.AreEqual(ExampleResponse,server.ToResponseString() + answer);

        }

        [Test]
        public void ShouldCalculateAnswerBytes()
        {
            var challengeBytes = Encoding.UTF8.GetBytes(Challenge);
            var challengeSegment = new ArraySegment<byte>(challengeBytes);
            var answerBytes = Draft76ResponseBuilder.CalculateAnswerBytes(Key1, Key2, challengeSegment);

            Assert.AreEqual(16, answerBytes.Length);

            var answer = Encoding.UTF8.GetString(answerBytes);

            Assert.AreEqual(ExpectedAnswer, answer);
        }
    }
}

