using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;

namespace Nugget.Tests
{
    [TestFixture]
    public class SendReceiveTest
    {
        private WebSocketServer wss;

        [TestFixtureSetUp]
        public void Initialize()
        {
            wss = new WebSocketServer(8181, "testproject", "ws://localhost:8181");
            Log.Level = LogLevel.Debug | LogLevel.Error | LogLevel.Info | LogLevel.Waring;
            wss.RegisterHandler<GetFromMe>("/get");
            wss.RegisterHandler<SendToMe>("/send");
            wss.RegisterHandler<EccoWithMe>("/ecco");
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
            SendToMe.Passed = false;
        }

        [TearDown]
        public void TearDown()
        {
        }


        class SendToMe : WebSocket
        {
            public static bool Passed = false;
            public static int Count = 500;

            public override void Incoming(string data)
            {
                Passed = (data.Length == Count);
            }

            public override void Disconnected()
            {
            }

            public override void Connected(ClientHandshake handshake)
            {
            }
        }

        class GetFromMe : WebSocket
        {
            public static bool Passed = false;
            public static int Count = 500;

            public override void Incoming(string data)
            {
            }

            public override void Disconnected()
            {
            }

            public override void Connected(ClientHandshake handshake)
            {
                var ys = new String('y', Count);
                Send(ys);
            }
        }

        class EccoWithMe : WebSocket
        {
            public override void Incoming(string data)
            {
                Send(data);
            }

            public override void Disconnected()
            {
            }

            public override void Connected(ClientHandshake handshake)
            {
            }
        }




        [Test]
        public void Send()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "testproject",
                Host = "localhost:8181",
                ResourcePath = "/send"
            });
            client.Send(new String('y', SendToMe.Count));
            Thread.Sleep(1000);
            Assert.IsTrue(SendToMe.Passed, "didn't send right");
        }

        [Test]
        public void Receive()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "testproject",
                Host = "localhost:8181",
                ResourcePath = "/get"
            });

            var answer = client.Receive();
            Assert.AreEqual(answer.Length, GetFromMe.Count, "didn't receive right");
        }

        [Test]
        public void Ecco()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "testproject",
                Host = "localhost:8181",
                ResourcePath = "/ecco"
            });
            var msg = "hvad drikker møller";
            client.Send(msg);
            var answer = client.Receive();
            Assert.AreEqual(msg, answer, "didn't ecco right");
        }

        [Test]
        public void SendMuch()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "testproject",
                Host = "localhost:8181",
                ResourcePath = "/send"
            });

            SendToMe.Count = 1000 * 1000;

            client.Send(new String('y', SendToMe.Count));
            Thread.Sleep(1000);
            Assert.IsTrue(SendToMe.Passed, "didn't send much right");
        }

        [Test]
        public void GetMuch()
        {
            var client = new ClientSocket(new ExtendedClientHandshake()
            {
                Origin = "testproject",
                Host = "localhost:8181",
                ResourcePath = "/get"
            });

            GetFromMe.Count = 1000 * 1000;

            var answer = client.Receive();
            Assert.AreEqual(GetFromMe.Count, answer.Length, "didn't get much right");
        }

        [Test]
        public void EccoMany()
        {
            bool wait = true;
            List<ClientSocket> clients = new List<ClientSocket>();

            int clientCount = 50;
            int doneCount = 1;
            for (int i = 0; i < clientCount; i++)
            {
                doneCount += i;
                clients.Add(new ClientSocket(new ExtendedClientHandshake()
                {
                    Origin = "testproject",
                    Host = "localhost:8181",
                    ResourcePath = "/ecco"
                }));
            }

            int doneYet = 1;
            bool passed = true;
            for (int i = 0; i < clientCount; i++)
            {
                var msg = "im number " + i.ToString();
                int j = i;
                clients[i].Send(msg);
                clients[i].ReceiveAsync((data) =>
                    {
                        passed = passed && (msg == data);
                        doneYet += j;
                        if (doneYet == doneCount)
                            wait = false;
                    });
            }

            while (wait)
            {
                Assert.IsTrue(passed);
            }

            
        }

    }
}

//  0,1,2
//1,1,2,4
/*
0,1,2,3,4,5

1,2,4,7,11

 
 
 * 
 5 = 11; 5*2 + 1   = 0+1+2+3+4 + 1
 6 = 16; 6*2 + 4   = 0+1+2+3+4+5 + 1 
 7 = 22; 7*2 + 7
 8 = 29: 8*2 + 13
 
 
 */

