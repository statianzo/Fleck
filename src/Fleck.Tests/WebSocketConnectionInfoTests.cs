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
            var info = WebSocketConnectionInfo.Create(request, clientIp);

            Assert.AreEqual(origin, info.Origin);
            Assert.AreEqual(host, info.Host);
            Assert.AreEqual(subprotocol, info.SubProtocol);
            Assert.AreEqual(path, info.Path);
            Assert.AreEqual(clientIp, info.ClientIpAddress);
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
            var info = WebSocketConnectionInfo.Create(request, null);

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

            var info = WebSocketConnectionInfo.Create(request, null);
            Assert.AreEqual(info.Cookies["chocolate"], "tasty");
            Assert.AreEqual(info.Cookies["cabbage"], "not so much");
        }

        [Test]
        public void ShouldHaveId()
        {
            var request = new WebSocketHttpRequest();
            var info = WebSocketConnectionInfo.Create(request, null);
            Assert.AreNotEqual(default(Guid), info.Id);
        }
    }
}
