﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://localhost:8181");
            
            Action<IWebSocketConnection> mySubProtocolInitializer = socket =>
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
                    };

            server.Start(mySubProtocolInitializer, new Dictionary<string, Action<IWebSocketConnection>>() { { "my-protocol", mySubProtocolInitializer } });

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
