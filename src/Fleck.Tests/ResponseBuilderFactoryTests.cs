using System;
using NUnit.Framework;
using Moq;
using Fleck.Interfaces;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class ResponseBuilderFactoryTests
    {
        [Test]
        public void ShouldResolveResponseBuilder()
        {
            var request = new WebSocketHttpRequest();
            var factory = new ResponseBuilderFactory();
            var mockBuilder = new Mock<IResponseBuilder>();
            mockBuilder
                .Setup(x => x.CanHandle(It.IsAny<WebSocketHttpRequest>()))
                .Returns(true);
            
            factory.Register(mockBuilder.Object);
            
            var builder = factory.Resolve(request);
            Assert.NotNull(builder);
        }
    }
}

