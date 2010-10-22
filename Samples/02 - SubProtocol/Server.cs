using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nugget;
using JONSParser;

namespace SubProtocol
{

    // the handler class
    // note that the class inherits the generic interface and uses the model as the type parameter
    class PostSocket : WebSocket<Post>
    {
        // this method is called when data is comming from the client
        // note that the method takes a Post object instead of a string
        // this is the object created in the model factory
        public override void Incoming(Post post)
        {
            Console.WriteLine("{0} posted {1}", post.Author, post.Body);
        }

        public override void Disconnected()
        {
            Console.WriteLine("--- disconnected ---");
        }

        public override void Connected(ClientHandshake handshake)
        {
            Console.WriteLine("--- connected ---");
        }
    }


    // a "model" representing a post made by someone
    class Post
    {
        public string Author { get; set; }
        public string Body { get; set; }

        public bool IsValid()
        {
            return !String.IsNullOrEmpty(Author) && 
                   !String.IsNullOrEmpty(Body);
        }
    }

    // the model factory
    // this class is used to convert the data comming from the client into an model object
    class PostFactory : ISubProtocolModelFactory<Post>
    {
        // create a new model
        // "data" is the data comming from the client
        // "connection" is an object representing the connection to the client, it can be used to identify the client if needed
        public Post Create(string data, WebSocketConnection connection)
        {
            // parse the data (we assume that the data string is on json format)
            var js = JSON.Parse(data);
            if (js != null && js.hasOwnProperty("author") && js.hasOwnProperty("body"))
            {
                // create a new model using the data from the client
                // "return" passes the model on to the handler class
                return new Post() { Author = js.author, Body = js.body };
            }
            return null;
        }

        // this method is used to define valid models
        // if a model is not valid, then it is not passed on to the handler class
        // this method is called internally in the server before the model is passed on
        public bool IsValid(Post p)
        {
            
            if (p == null)
                return false;
            else
                return p.IsValid();
        }
    }

    class Server
    {
        static void Main(string[] args)
        {
            // create the server
            var nugget = new WebSocketServer(8181, "null", "ws://localhost:8181");
            // register the handler
            nugget.RegisterHandler<PostSocket>("/subsample");
            // register the model factory
            // this is important since we need the model factory to create the models
            // the string passed is the name of the sub protocol that the factory should be used for
            // any client connecting with this subprotocol, will have all its data send throught the factory
            nugget.SetSubProtocolModelFactory(new PostFactory(), "post");
            
            // start the server
            nugget.Start();
            // info
            Console.WriteLine("Server started, open client.html in a websocket-enabled browser");
            
            // keep alive
            var input = Console.ReadLine();
            while (input != "exit")
            {
                input = Console.ReadLine();
            }

        }
    }
}
