using NUnit.Framework;

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
            var request =
                new WebSocketHttpRequest
                    {
                        Headers =
                            {
                                {"Origin", origin},
                                {"Host", host},
                                {"Sec-WebSocket-Protocol", subprotocol}
                            }
                    };
            var info = WebSocketConnectionInfo.Create(request);

            Assert.AreEqual(origin, info.Origin);
            Assert.AreEqual(host, info.Host);
            Assert.AreEqual(subprotocol, info.SubProtocol);
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

            var info = WebSocketConnectionInfo.Create(request);
            Assert.AreEqual(info.Cookies["chocolate"], "tasty");
            Assert.AreEqual(info.Cookies["cabbage"], "not so much");
        }
    }
}