using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;

namespace ConsoleApp
{
    class Server
    {
        static void Main()
        {
        	var allSockets = new List<WebSocketConnection>();
            var server = new WebSocketServer(8181, "null", "ws://localhost:8181");
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
					socket.OnMessage = message => Console.WriteLine(message);
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
