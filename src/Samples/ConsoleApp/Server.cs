using System;
using System.Collections.Generic;
using Fleck;
using System.Net.Sockets;
using System.Linq;

class Program
{
    static void Main()
    {
        FleckLog.Level = LogLevel.Debug;
        FleckLog.LogAction = (level, message, ex) =>
        {
            if (level >= FleckLog.Level)
                Console.WriteLine("{0} [{1}] {2} {3}", DateTime.Now, level, message, ex?.Message);
        };

        var allSockets = new List<IWebSocketConnection>();
        var server = new WebSocketServer("ws://0.0.0.0:8181");

        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            if (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine("The port is already in use. Please use a different port.");
            }
        }
    }
}