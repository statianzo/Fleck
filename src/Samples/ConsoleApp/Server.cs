using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            testRelay();
        }

        static void testDefault()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
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

        static void testRelay()
        {
            FleckLog.Level = LogLevel.Debug;
            var relay = new WebSocketRelay();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    relay.Add(socket);
                    socket.Send("Echo: You have Joined the Group [" + relay.Name + "]");
                    relay.Broadcast("Relay: " + socket.ConnectionInfo.Id.ToString() + " Joined", socket.ConnectionInfo.Id);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    relay.Remove(socket);
                    relay.Broadcast("Relay [" + socket.ConnectionInfo.Id.ToString() + "]: " + socket.ConnectionInfo.Id.ToString() + " Left", socket.ConnectionInfo.Id);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                    socket.Send("Echo: " + message);
                    relay.Broadcast("Relay [" + socket.ConnectionInfo.Id.ToString() + "]: " + message, socket.ConnectionInfo.Id);
                };
            });

            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in relay.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }
    }
}
