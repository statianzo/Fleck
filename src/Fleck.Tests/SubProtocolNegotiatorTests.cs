using System;
using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class SubProtocolNegotiatorTests
    {
        [Test]
        public void ShouldReturnNullWhenServerProtocolsEmpty()
        {
            var server = new string[0];
            var client = new string[] { "x", "y" };
            Assert.Null(SubProtocolNegotiator.Negotiate(server, client));
        }

        [Test]
        public void ShouldReturnNullWhenClientProtocolsEmpty()
        {
            var server = new string[] { "q", "r" };
            var client = new string[0];
            Assert.Null(SubProtocolNegotiator.Negotiate(server, client));
        }

        [Test]
        public void ShouldReturnFirstClientMatch()
        {
            var server = new string[] { "a", "b", "c"};
            var client = new string[] { "b", "a"};
            Assert.AreEqual("b", SubProtocolNegotiator.Negotiate(server, client));
        }

        [Test]
        public void ShouldThrowOnNoMatchesOfServer()
        {
            var server = new string[] { "a", "b", "c"};
            var client = new string[] { "z"};
            Assert.Throws<SubProtocolNegotiationFailureException>(() => SubProtocolNegotiator.Negotiate(server, client));
        }
    }
}

