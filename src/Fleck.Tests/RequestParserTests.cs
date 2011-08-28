using System;
using NUnit.Framework;
using Fleck.Tests;
using System.Text;

namespace Fleck.Tests
{
  [TestFixtureAttribute]
  public class RequestParserTests
  {
  
    RequestParser _parser;
  
    [SetUp]
    public void Setup()
    {
      _parser = new RequestParser();
    }
    
    [Test]
    public void ShouldReturnRequest()
    {
      
      var bytes = new ArraySegment<byte>(new byte[0], 0, 0);
      WebSocketHttpRequest request = _parser.Parse(bytes);
      
      Assert.IsNotNull(request);
    }
    
        const string body =
@"GET /demo HTTP/1.1
Host: example.com
Connection: Upgrade
Sec-WebSocket-Key2: 12998 5 Y3 1  .P00
Sec-WebSocket-Protocol: sample
Upgrade: WebSocket
Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5
Origin: http://example.com

^n:ds[4U";
    
    [Test]
    public void ShouldReadResourceLine()
    {
      var bodyBytes = Encoding.UTF8.GetBytes(body);
      
      var bytes = new ArraySegment<byte>(bodyBytes);
      WebSocketHttpRequest request = _parser.Parse(bytes);
      
      Assert.AreEqual("GET", request.Method);
      Assert.AreEqual("/demo", request.Path);
    }
    
    [Test]
    public void ShouldReadHeaders()
    {
      var bodyBytes = Encoding.UTF8.GetBytes(body);
      
      var bytes = new ArraySegment<byte>(bodyBytes);
      WebSocketHttpRequest request = _parser.Parse(bytes);
      
      Assert.AreEqual("example.com", request.Headers["Host"]);
      Assert.AreEqual("Upgrade", request.Headers["Connection"]);
      Assert.AreEqual("12998 5 Y3 1  .P00", request.Headers["Sec-WebSocket-Key2"]);
      Assert.AreEqual("http://example.com", request.Headers["Origin"]);
    }
  }
}

