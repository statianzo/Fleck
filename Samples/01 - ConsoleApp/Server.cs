using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nugget;

namespace ConsoleApp
{

    // The server side socket
    class ConsoleAppSocket : WebSocket
    {
        // This method is called when data is comming from the client.
        // In this example the method is just empty
        public override void Incoming(string data)
        {

        }

        // This method is called when the socket disconnects
        public override void Disconnected()
        {
            Console.WriteLine("--- disconnected ---");
        }

        // This method is called when the socket connects
        public override void Connected(ClientHandshake handshake)
        {
            Console.WriteLine("--- connected --- ");
        }
    }
    

    class Server
    {
        static void Main(string[] args)
        {
            // create the server
            // the parameters describe where to listen for connections (the port) and which connections to accept (the origin and location)
            // it is important that these are correct, or the server might not accept the incoming connections
            // see http://tools.ietf.org/html/draft-hixie-thewebsocketprotocol, to learn more about these parameters
            var nugget = new WebSocketServer(8181, "null", "ws://localhost:8181");

            // register the ConsoleAppSocket class to handle connection comming to /consoleappsample
            nugget.RegisterHandler<ConsoleAppSocket>("/consoleappsample");
            // no logging
            Nugget.Log.Level = LogLevel.None;
            // start the server
            nugget.Start();
            
            // info
            Console.WriteLine("Server started, open client.html in a websocket-enabled browser\n");
            Console.WriteLine("Once the connection is established\nanything you type into this console, will be send to the browser");


            // keep alive loop
            var input = Console.ReadLine();
            while (input != "exit")
            {
                nugget.SendToAll(input);                
                input = Console.ReadLine();
            }

        }
    }
}
