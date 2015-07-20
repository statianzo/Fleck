using NUnit.Framework;
using System.Text;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class RequestParserTests
    {
        [Test]
        public void ShouldReturnNullForEmptyBytes()
        {
            WebSocketHttpRequest request = RequestParser.Parse(new byte[0]);
      
            Assert.IsNull(request);
        }
    
        [Test]
        public void ShouldReadResourceLine()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());
      
            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("/demo", request.Path);
        }
    
        [Test]
        public void ShouldReadHeaders()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());
      
            Assert.AreEqual("example.com", request.Headers ["Host"]);
            Assert.AreEqual("Upgrade", request.Headers ["Connection"]);
            Assert.AreEqual("12998 5 Y3 1  .P00", request.Headers ["Sec-WebSocket-Key2"]);
            Assert.AreEqual("http://example.com", request.Headers ["Origin"]);
        }
    
        [Test]
        public void ShouldReadBody()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());
      
            Assert.AreEqual("^n:ds[4U", request.Body);
        }
    
        [Test]
        public void ValidRequestShouldNotBeNull()
        {
            Assert.NotNull(RequestParser.Parse(ValidRequestArray()));
        }
    
        [Test]
        public void NoBodyRequestShouldNotBeNull()
        {
            const string noBodyRequest =
        "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noBodyRequest);
      
            Assert.IsNotNull(RequestParser.Parse(bytes));
        }
    
        [Test]
        public void NoHeadersRequestShouldBeNull()
        {
            const string noHeadersNoBodyRequest =
        "GET /zing HTTP/1.1\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noHeadersNoBodyRequest);
      
            Assert.IsNull(RequestParser.Parse(bytes));
        }
    
        [Test]
        public void HeadersShouldBeCaseInsensitive()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());

            Assert.IsTrue(request.Headers.ContainsKey("Sec-WebSocket-Protocol"));
            Assert.IsTrue(request.Headers.ContainsKey("sec-websocket-protocol"));
            Assert.IsTrue(request.Headers.ContainsKey("sec-WEBsocket-protoCOL"));
            Assert.IsTrue(request.Headers.ContainsKey("UPGRADE"));
            Assert.IsTrue(request.Headers.ContainsKey("CONNectiON"));
        }
    
        [Test]
        public void PartialHeaderRequestShouldBeNull()
        {
            const string partialHeaderRequest =
        "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSoc"; //Cut off
            var bytes = RequestArray(partialHeaderRequest);
      
            Assert.IsNull(RequestParser.Parse(bytes));
        }
    
        [Test]
        public void EmptyHeaderValuesShouldParse()
        {
            const string emptyCookieRequest =
                "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "Cookie: \r\n" +
                "User-Agent:\r\n" +     //no space after colon
                "\r\n" +
                "^n:ds[4U";
            var bytes = RequestArray(emptyCookieRequest);
            var request = RequestParser.Parse(bytes);
            Assert.IsNotNull(request);
            Assert.AreEqual("", request.Headers ["Cookie"]);
        }

        public byte[] ValidRequestArray()
        {
            return RequestArray(validRequest);
        }
    
        public byte[] RequestArray(string request)
        {
            return Encoding.UTF8.GetBytes(request);
        }
    
        const string validRequest =
"GET /demo HTTP/1.1\r\n" +
            "Host: example.com\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
            "Sec-WebSocket-Protocol: sample\r\n" +
            "Upgrade: WebSocket\r\n" +
            "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
            "Origin: http://example.com\r\n" +
            "\r\n" +
            "^n:ds[4U";



    }
}

