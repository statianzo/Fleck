namespace Fleck.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ClientHandshakeTest
    {
        [Test]
        public void HostnameShouldMatchOnUri()
        {
            var clientHandshake = new ClientHandshake();
            clientHandshake.Key1 = "aaa";
            clientHandshake.Key2 = "bbb";
            clientHandshake.Origin = "AAA";
            clientHandshake.ResourcePath = "BBB";

            clientHandshake.Host = "localhost:8181";
            Assert.IsTrue(clientHandshake.Validate(null, "ws://localhost:8181/"));
        }
    }
}