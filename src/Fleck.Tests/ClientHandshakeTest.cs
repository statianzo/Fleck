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
            Assert.IsTrue(clientHandshake.Validate(null, "ws://localhost:8181/", "ws"));
        }

        [Test]
        public void CorruptHostShouldNotValidate()
        {
            var clientHandshake = new ClientHandshake();
            clientHandshake.Key1 = "aaa";
            clientHandshake.Key2 = "bbb";
            clientHandshake.Origin = "AAA";
            clientHandshake.ResourcePath = "BBB";

            clientHandshake.Host = "$%%$%NoT^^^A)()(()VALID--==!!URI&&&@@#$#~~~";
            Assert.IsFalse(clientHandshake.Validate(null, "ws://localhost:8181/", "ws"));
        }

        [Test]
        public void NullHostShouldNotValidate()
        {
            var clientHandshake = new ClientHandshake();
            clientHandshake.Key1 = "aaa";
            clientHandshake.Key2 = "bbb";
            clientHandshake.Origin = "AAA";
            clientHandshake.ResourcePath = "BBB";

            clientHandshake.Host = null;
            Assert.IsFalse(clientHandshake.Validate(null, "ws://localhost:8181/", "ws"));
        }
    }
}