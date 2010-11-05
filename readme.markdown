Fleck
===

Fleck is a websocket implementation in C#. Branched from the [Nugget][nugget]
project, Fleck requires no inheritence, or reference to Unity. 

Example
---

The following is an example that will echo to a client.

      var server = new WebSocketServer(8181, "ws://localhost:8181");
      server.Start(socket =>
        {
          socket.OnOpen = () => Console.WriteLine("Open!");
          socket.OnClose = () => Console.WriteLine("Close!");
          socket.OnMessage = message => socket.Send(message);
        });

License
---

The MIT License

Copyright (c) 2010 Jason Staten

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.



[nugget]: http://nugget.codeplex.com/ 
