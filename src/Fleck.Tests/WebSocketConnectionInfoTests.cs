using NUnit.Framework;
using System;

namespace Fleck.Tests
{
    [TestFixture]
    public class WebSocketConnectionInfoTests
    {
        [Test]
        public void ShouldReadHeadersFromRequest()
        {
            const string origin = "http://blah.com/path/to/page";
            const string host = "blah.com";
            const string subprotocol = "Submarine!";
            const string path = "/path/to/page";
            const string clientIp = "127.0.0.1";
            const int clientPort = 0;
            const string negotiatedSubProtocol = "Negotiated";

            var request =
                new WebSocketHttpRequest
                    {
                        Headers =
                            {
                                {"Origin", origin},
                                {"Host", host},
                                {"Sec-WebSocket-Protocol", subprotocol}
                            },
                        Path = path
                    };
            var info = WebSocketConnectionInfo.Create(request, clientIp, clientPort, negotiatedSubProtocol);

            Assert.AreEqual(origin, info.Origin);
            Assert.AreEqual(host, info.Host);
            Assert.AreEqual(subprotocol, info.SubProtocol);
            Assert.AreEqual(path, info.Path);
            Assert.AreEqual(clientIp, info.ClientIpAddress);
            Assert.AreEqual(negotiatedSubProtocol, info.NegotiatedSubProtocol);
        }

        [Test]
        public void ShouldProvideAdditionalHeaders()
        {
            const string origin = "http://blah.com/path/to/page";
            const string host = "blah.com";
            const string subprotocol = "Submarine!";
            const string username = "Username";
            const string secret = "Secret";
            const string clientIp = "127.0.0.1";
            const string cookies = "chocolate=yummy; oatmeal=alsoyummy";
            const int clientPort = 0;
            const string negotiatedSubProtocol = "Negotiated";

            var request =
                new WebSocketHttpRequest
                {
                    Headers =
                    {
                        {"Origin", origin},
                        {"Host", host},
                        {"Sec-WebSocket-Protocol", subprotocol},
                        {"Username", username},
                        {"Cookie", cookies}
                    }
                };

            var info = WebSocketConnectionInfo.Create(request, clientIp, clientPort, negotiatedSubProtocol);

            var headers = info.Headers;
            string usernameValue = null;

            Assert.IsNotNull(headers);
            Assert.AreEqual(5,headers.Count);
            Assert.True(headers.TryGetValue("Username", out usernameValue));
            Assert.True(usernameValue.Equals(username));
            Assert.True(headers.ContainsKey("Cookie"));
        }

        [Test]
        public void ShouldReadSecWebSocketOrigin()
        {
            const string origin = "http://example.com/myPath";
            var request =
                new WebSocketHttpRequest
                    {
                        Headers = { {"Sec-WebSocket-Origin", origin} }
                    };
            var info = WebSocketConnectionInfo.Create(request, null, 1, null);

            Assert.AreEqual(origin, info.Origin);
        }

        [Test]
        public void ShouldParseCookies()
        {
            const string cookie = "chocolate=tasty; cabbage=not so much";
            var request =
                new WebSocketHttpRequest
                    {
                        Headers = { {"Cookie", cookie} }
                    };

            var info = WebSocketConnectionInfo.Create(request, null, 1, null);
            Assert.AreEqual(info.Cookies["chocolate"], "tasty");
            Assert.AreEqual(info.Cookies["cabbage"], "not so much");
        }

        [Test]
        public void ShouldParseCookiesWithoutSpaces()
        {
            const string cookie = "chocolate=tasty;cabbage=not so much;";
            var request =
                new WebSocketHttpRequest
                {
                    Headers = { { "Cookie", cookie } }
                };

            var info = WebSocketConnectionInfo.Create(request, null, 1, null);
            Assert.AreEqual(info.Cookies["chocolate"], "tasty");
            Assert.AreEqual(info.Cookies["cabbage"], "not so much");
        }

        [Test]
        public void ShouldHaveId()
        {
            var request = new WebSocketHttpRequest();
            var info = WebSocketConnectionInfo.Create(request, null, 1, null);
            Assert.AreNotEqual(default(Guid), info.Id);
        }
    }
}
