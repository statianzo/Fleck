using System;
using NUnit.Framework;
using Fleck.Interfaces;
using Moq;

namespace Fleck.Tests
{
    [TestFixture]
    public class DefaultHandlerFactoryTests
    {
        DefaultHandlerFactory _factory;
        MockRepository _mockRepository;
        
        [SetUp]
        public void Setup()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _factory = new DefaultHandlerFactory("");
        }
        
        [TearDown]
        public void TearDown()
        {
            _mockRepository.Verify();
        }
        
        [Test]
        public void ShouldReturnNullForIncompleteHeaders()
        {
            var parser = _mockRepository.Create<IRequestParser>();
            parser
                .Setup(x => x.IsComplete(It.IsAny<byte[]>()))
                .Returns(false)
                .Verifiable();
            
            _factory.RequestParser = parser.Object;
            
            var handler = _factory.BuildHandler(new byte[0], x => {});
            
            Assert.IsNull(handler);
        }
        
        [Test]
        public void ShouldReturnHandlerForValidHeaders()
        {
            var parser = _mockRepository.Create<IRequestParser>();
            parser
                .Setup(x => x.IsComplete(It.IsAny<byte[]>()))
                .Returns(true)
                .Verifiable();
                
            parser.Setup(x => x.Parse(It.IsAny<byte[]>()))
                .Returns(new WebSocketHttpRequest {Headers = {{"Valid", "Request"}}});
            
            _factory.RequestParser = parser.Object;
            
            var handler = _factory.BuildHandler(new byte[0], x => {});
            
            Assert.IsNotNull(handler);
        }
        
        [Test]
        public void ShouldThrowWhenUnsupportedType()
        {
            var parser = _mockRepository.Create<IRequestParser>();
            parser
                .Setup(x => x.IsComplete(It.IsAny<byte[]>()))
                .Returns(true)
                .Verifiable();
            
            parser.Setup(x => x.Parse(It.IsAny<byte[]>()))
                .Returns(new WebSocketHttpRequest {Headers = {{"Bad", "Request"}}});
            
            _factory.RequestParser = parser.Object;
            
            Assert.Throws<WebSocketException>(() => _factory.BuildHandler(new byte[0], x => {}));
            
        }
    }
}

