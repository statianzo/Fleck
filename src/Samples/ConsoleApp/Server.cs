using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            //Test1();
            Test2();
        }

        static WebSocketServer mServer;
        static List<IWebSocketConnection> mClients;
        //static Action<IWebSocketConnection> mAction;

        static void Test2()
        {
            FleckLog.Level = LogLevel.Debug;

            mClients = new List<IWebSocketConnection>();

            string SSLFileName;
            string SSLPassword;

            SSLFileName = @"C:\Astros\AstrosSocket\astros.com.pfx";
            SSLPassword = "c0sm0s";

            string WSLocation;

            //WSLocation = "ws://localhost:8181";
            //WSLocation = "wss://dev.astros.com:8181";
            //WSLocation = "ws://dev.astros.com:11234";
            WSLocation = "wss://dev.astros.com:11234";

            mServer = new WebSocketServer(WSLocation);
            mServer.Certificate = new X509Certificate2(SSLFileName, SSLPassword);
            
            mServer.Start(socket =>
            {
                socket.OnError = ex =>
                {
                    Console.WriteLine("Error: " + ex.Message);
                };
                socket.OnOpen = () =>
                {
                    IWebSocketConnectionInfo conn = socket.ConnectionInfo;
                    Console.WriteLine("Open! (" + conn.ClientIpAddress + ":" + conn.ClientPort.ToString() + ")");
                    mClients.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    mClients.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                    mClients.ToList().ForEach(s => s.Send("Echo: " + message));
                };
            });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (IWebSocketConnection socket in mClients.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }

        static void Test1()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://localhost:8181");
            server.Start(socket =>
                {
                    socket.OnOpen = () =>
                        {
                            Console.WriteLine("Open!");
                            allSockets.Add(socket);
                        };
                    socket.OnClose = () =>
                        {
                            Console.WriteLine("Close!");
                            allSockets.Remove(socket);
                        };
                    socket.OnMessage = message =>
                        {
                            Console.WriteLine(message);
                            allSockets.ToList().ForEach(s => s.Send("Echo: " + message));
                        };
                });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }
    }
}
