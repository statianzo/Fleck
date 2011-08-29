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
    
    [Test]
    public void ShouldReadResourceLine()
    {
      WebSocketHttpRequest request = _parser.Parse(ValidRequestArraySegment());
      
      Assert.AreEqual("GET", request.Method);
      Assert.AreEqual("/demo", request.Path);
    }
    
    [Test]
    public void ShouldReadHeaders()
    {
      WebSocketHttpRequest request = _parser.Parse(ValidRequestArraySegment());
      
      Assert.AreEqual("example.com", request.Headers["Host"]);
      Assert.AreEqual("Upgrade", request.Headers["Connection"]);
      Assert.AreEqual("12998 5 Y3 1  .P00", request.Headers["Sec-WebSocket-Key2"]);
      Assert.AreEqual("http://example.com", request.Headers["Origin"]);
    }
    
    [Test]
    public void ShouldReadBody()
    {
      WebSocketHttpRequest request = _parser.Parse(ValidRequestArraySegment());
      
      Assert.AreEqual("^n:ds[4U", request.Body);
    }
    
    
    [Test]
    public void ValidRequestShouldBeComplete()
    {
      Assert.True(_parser.IsComplete(ValidRequestArraySegment()));
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
      var bytes = RequestArraySegment(noBodyRequest);
      
      Assert.True(_parser.IsComplete(bytes));
    }
    
    [Test]
    public void EmptyRequestShouldNotBeComplete()
    {
      var bytes = new ArraySegment<byte>(new byte[0], 0, 0);
      Assert.False(_parser.IsComplete(bytes));
    }
    
    [Test]
    public void NoHeadersRequestShouldNotBeComplete()
    {
      const string noHeadersNoBodyRequest =
        "GET /zing HTTP/1.1\r\n" +
        "\r\n" +
        "";
      var bytes = RequestArraySegment(noHeadersNoBodyRequest);
      
      Assert.False(_parser.IsComplete(bytes));
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
      var bytes = RequestArraySegment(partialHeaderRequest);
      
      Assert.False(_parser.IsComplete(bytes));
    }
    
    public ArraySegment<byte> ValidRequestArraySegment()
    {
      return RequestArraySegment(validRequest);
    }
    
    public ArraySegment<byte> RequestArraySegment(string request)
    {
      var bodyBytes = Encoding.UTF8.GetBytes(request);
      return new ArraySegment<byte>(bodyBytes);
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

