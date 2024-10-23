using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            //Using concurrent bag instead of list 
            //This gives a thread - safe collection by adding locks when modifying allSockets
            var allSockets = new ConcurrentBag<IWebSocketConnection>();
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
                            allSockets.TryTake(out socket);
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
                foreach (var socket in allSockets)
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }
    }
}
