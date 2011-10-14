using NUnit.Framework;
using System.Text;

namespace Fleck.Tests
{
  [TestFixtureAttribute]
  public class RequestParserTests
  {
    [Test]
    public void ShouldReturnRequest()
    {
      WebSocketHttpRequest request = RequestParser.Parse(new byte[0]);
      
      Assert.IsNotNull(request);
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
      
      Assert.AreEqual("example.com", request.Headers["Host"]);
      Assert.AreEqual("Upgrade", request.Headers["Connection"]);
      Assert.AreEqual("12998 5 Y3 1  .P00", request.Headers["Sec-WebSocket-Key2"]);
      Assert.AreEqual("http://example.com", request.Headers["Origin"]);
    }
    
    [Test]
    public void ShouldReadBody()
    {
      WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());
      
      Assert.AreEqual("^n:ds[4U", request.Body);
    }
    
    
    [Test]
    public void ValidRequestShouldBeComplete()
    {
      Assert.True(RequestParser.IsComplete(ValidRequestArray()));
    }
    
    [Test]
    public void NoBodyRequestShouldBeComplete()
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
      
      Assert.True(RequestParser.IsComplete(bytes));
    }
    
    [Test]
    public void EmptyRequestShouldNotBeComplete()
    {
      Assert.False(RequestParser.IsComplete(new byte[0]));
    }
    
    [Test]
    public void NoHeadersRequestShouldNotBeComplete()
    {
      const string noHeadersNoBodyRequest =
        "GET /zing HTTP/1.1\r\n" +
        "\r\n" +
        "";
      var bytes = RequestArray(noHeadersNoBodyRequest);
      
      Assert.False(RequestParser.IsComplete(bytes));
    }
    
    [Test]
    public void PartialHeaderRequestShouldNotBeComplete()
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
      
      Assert.False(RequestParser.IsComplete(bytes));
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

