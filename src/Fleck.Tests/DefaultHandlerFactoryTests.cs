using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class DefaultHandlerFactoryTests
    {
        [Test]
        public void ShouldReturnHandlerForValidHeaders()
        {
            var request = new WebSocketHttpRequest {Headers = {{"Sec-WebSocket-Key1", "BLAH"}}};
            var handler = HandlerFactory.BuildHandler(request, x => { }, () => { }, x => { }, x => { }, x => { });
            
            Assert.IsNotNull(handler);
        }
        
        [Test]
        public void ShouldThrowWhenUnsupportedType()
        {
            
            var request = new WebSocketHttpRequest {Headers = {{"Bad", "Request"}}};
            Assert.Throws<WebSocketException>(() => HandlerFactory.BuildHandler(request, x => {}, () => {}, x => { }, x => { }, x => { }));
            
        }
    }
}

