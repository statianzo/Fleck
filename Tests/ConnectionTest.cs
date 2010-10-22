using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;

namespace Nugget.Tests
{
    [TestFixture]
    public class ConnectionTest
    {
        private WebSocketServer wss;

        [TestFixtureSetUp]
        public void Initialize()
        {
            wss = new WebSocketServer(8181, "testproject", "ws://localhost:8181");
            wss.RegisterHandler<TestSocket>("/test");
            wss.Start();
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            wss.Dispose();
        }


        [SetUp]
        public void Setup()
        {
            TestSocket.ConnectedCalled = false;
            TestSocket.IncomingCalled = false;
            TestSocket.DisconnectedCalled = false;
        }

        [TearDown]
        public void TearDown()
        {
        }


        class TestSocket : WebSocket
        {
            public static bool IncomingCalled = false;
            public static bool DisconnectedCalled = false;
            public static bool ConnectedCalled = false;

            public override void Incoming(string data)
            {
                IncomingCalled = true;
            }

            public override void Disconnected()
            {
                DisconnectedCalled = true;
            }

            public override void Connected(ClientHandshake handshake)
            {
                ConnectedCalled = true;
            }
        }

        [Test]
        public void ConnectTest()
        {
            var client = new ClientSocket(new ExtendedClientHandshake() 
            { 
                Origin = "testproject", 
                Host = "localhost:8181", 
                ResourcePath = "/test" 
            });
            Thread.Sleep(1000); // allow the socket some time to connect


            Assert.IsTrue(TestSocket.ConnectedCalled, "sokcet didn't connect");

        }

        [Test]
        public void InvalidOriginTest()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "not test project",
                Host = "localhost:8181",
                ResourcePath = "/test"
            });
            Thread.Sleep(1000); // allow the socket some time to connect

            Assert.IsFalse(TestSocket.ConnectedCalled, "socket connected with wrong origin");
        }

        [Test]
        public void InvalidChallengeTest()
        {
            // challenge that will (most likely) not fit with the keys of the handshake
            var challenge = new ArraySegment<byte>(new byte[] {1,3,3,4,5,6,7,8});
                
            var hs = new ExtendedClientHandshake() 
                { 
                    Origin = "testproject", 
                    Host = "localhost:8181", 
                    ResourcePath = "/test", 
                    ChallengeBytes = challenge // wrong challenge
                };

            hs.ChallengeBytes = challenge;
            var client = new ClientSocket(hs);
            Thread.Sleep(1000);
            
            Assert.IsTrue(TestSocket.DisconnectedCalled, "socket didn't disconnect when it should have");
        }

        [Test]
        public void NoPathTest()
        {
            var client = new ClientSocket(new ExtendedClientHandshake() 
            { 
                Origin = "testproject", 
                Host = "localhost:8181", 
                ResourcePath = "not /test" // path not registered
            });
            Thread.Sleep(1000); // allow the socket some time to connect

            Assert.IsFalse(TestSocket.ConnectedCalled, "socket connected with wrong path");
        }

    }
}
