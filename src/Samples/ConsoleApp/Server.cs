using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8000");
            server.Start(socket =>
                {
                    socket.OnOpen = () =>
                        {
                            Console.WriteLine("Open!");
                            var ci = socket.ConnectionInfo;
                            var msg = $"Connect from [{ci.ClientIpAddress}:{ci.ClientPort}]";
                            socket.Send(msg);
                            lock(allSockets)
                                allSockets.Add(socket);
                        };
                    socket.OnClose = () =>
                        {
                            Console.WriteLine("Close!");
                            lock (allSockets)
                                allSockets.Remove(socket);
                        };
                    socket.OnMessage = message =>
                        {
                            Console.WriteLine(message);
                            lock (allSockets)
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
