using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.Tests
{
    [TestFixture]
    public class HandshakeHandlerTests
    {
        private HandshakeHandler _handler;

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
        //From http://www.whatwg.org/specs/web-socket-protocol/ Page 4
        const string Key1 = "4 @1  46546xW%0l 1 5";
        const string Key2 = "12998 5 Y3 1  .P00";
        const string Challenge = "^n:ds[4U";
        const string ExpectedAnswer = "8jKS'y:G*Co,Wxa-";

        [SetUp]
        public void Setup()
        {
            _handler = new HandshakeHandler(null, "ws://fleck-test.com", "ws");
        }

        [Test]
        public void ShouldShake()
        {
            var fakeSocket = new FakeSocket(ExampleRequest);
            _handler.Shake(fakeSocket);
        }


        [Test]
        public void ShouldParseClientHandshake()
        {
            var requestBytes = Encoding.UTF8.GetBytes(ExampleRequest);
            var requestSegment = new ArraySegment<byte>(requestBytes);
            var client = HandshakeHandler.ParseClientHandshake(requestSegment);

            Assert.AreEqual(Key1, client.Key1);
            Assert.AreEqual(Key2, client.Key2);
            Assert.AreEqual("http://example.com", client.Origin);
            Assert.AreEqual("example.com", client.Host);
            var clientChallenge = client.ChallengeBytes.Array.Skip(client.ChallengeBytes.Offset).Take(client.ChallengeBytes.Count).ToArray();
            var clientChallengeString = Encoding.UTF8.GetString(clientChallenge);
            Assert.AreEqual(Challenge, clientChallengeString);
        }

        [Test]
        public void ShouldGenerateServerHandshake()
        {
            var requestBytes = Encoding.UTF8.GetBytes(ExampleRequest);
            var requestSegment = new ArraySegment<byte>(requestBytes);
            var client = HandshakeHandler.ParseClientHandshake(requestSegment);
            _handler.ClientHandshake = client;
            var server = _handler.GenerateResponseHandshake();

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
            var answerBytes = HandshakeHandler.CalculateAnswerBytes(Key1, Key2, challengeSegment);

            Assert.AreEqual(16, answerBytes.Length);

            var answer = Encoding.UTF8.GetString(answerBytes);

            Assert.AreEqual(ExpectedAnswer, answer);
        }
    }

    public class FakeSocket : ISocket
    {
        private readonly string _request;

        public FakeSocket(string request)
        {
            _request = request;
        }

        public bool Connected
        {
            get { throw new NotImplementedException(); }
        }

        public Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public Task Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error)
        {
            var bytes = Encoding.UTF8.GetBytes(_request);
            bytes.CopyTo(buffer, 0);
            callback(bytes.Length);
            return new Task<int>(() => bytes.Length);
        }

        Task ISocket.Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Console.WriteLine("Closed");
        }

        public void Bind(EndPoint ipLocal)
        {
            throw new NotImplementedException();
        }

        public void Listen(int backlog)
        {
            throw new NotImplementedException();
        }

        public void Authenticate (X509Certificate2 certificate, Action callback, Action<Exception> error)
        {
          throw new NotImplementedException ();
        }
    }
}