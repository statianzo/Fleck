using System;
using System.Text;
using Fleck.Handlers;
using NUnit.Framework;
using System.Linq;

namespace Fleck.Tests
{
    [TestFixture]
    public class LimitedHybi13HandlerTests
    {
        private IHandler _handler;
        private WebSocketHttpRequest _request;
        private Action<string> _onMessage;
        private Action<byte[]> _onBinary;
        private Action<byte[]> _onPing;
        private Action<byte[]> _onPong;
        private Action _onClose;

        [SetUp]
        public void Setup()
        {
            _request = new WebSocketHttpRequest();
            _onClose = delegate { };
            _onMessage = delegate { };
            _onBinary = delegate { };
            _onPing = delegate { };
            _onPong = delegate { };

            _handler = Hybi13Handler.Create(_request, new HandlerSettings {Hybi13MaxMessageSize = 256}, s => _onMessage(s), () => _onClose(), b => _onBinary(b), b => _onPing(b), b => _onPong(b));
        }

        [Test]
        public void ShouldThrowWhenBinaryMessageLongerThanLimit()
        {
            var frame = new Hybi14DataFrame
            {
                FrameType = FrameType.Binary,
                IsFinal = true,
                IsMasked = true,
                MaskKey = 234234,
                Payload = new byte[257]
            };
            
            Assert.Catch<WebSocketException>(() => _handler.Receive(frame.ToBytes()));
        }
        
        [Test]
        public void ShouldThrowWhenTextMessageLongerThanLimit()
        {
            var frame = new Hybi14DataFrame
            {
                FrameType = FrameType.Text,
                IsFinal = true,
                IsMasked = true,
                MaskKey = 234234,
                Payload = Encoding.UTF8.GetBytes(new string('+', 257))
            };
            
            Assert.Catch<WebSocketException>(() => _handler.Receive(frame.ToBytes()));
        }
        
        private const string ExampleRequest =
"GET /chat HTTP/1.1\r\n" +
"Host: server.example.com\r\n" +
"Upgrade: websocket\r\n" +
"Connection: Upgrade\r\n" +
"Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
"Origin: http://example.com\r\n" +
"Sec-WebSocket-Protocol: chat, superchat\r\n" +
"Sec-WebSocket-Version: 13\r\n" +
"\r\n";

        private const string ExampleResponse =
"HTTP/1.1 101 Switching Protocols\r\n" +
"Upgrade: websocket\r\n" +
"Connection: Upgrade\r\n" +
"Sec-WebSocket-Protocol: superchat\r\n" +
"Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n" +
"\r\n";
    }
}
