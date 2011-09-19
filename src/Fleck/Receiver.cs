using System;
using System.Collections.Generic;
using Fleck.Interfaces;

namespace Fleck
{
    public class Receiver : IReceiver
    {
        public event Action OnError = delegate { };
        public event Action<string> OnMessage;
        
        private const int BufferSize = 16384;
        private readonly Queue<byte> _queue;
        private readonly ISocket _socket;

        public Receiver(ISocket socket)
        {
            _socket = socket;
            _queue = new Queue<byte>();
        }

        private ISocket Socket
        {
            get { return _socket; }
        }
        
        public void Receive()
        {
            Receive(new DataFrame());
        }

        public void Receive(DataFrame frame)
        {
            var buffer = new byte[BufferSize];

            if (Socket == null || !Socket.Connected)
            {
                OnError();
                return;
            }

            Socket
                .Receive(buffer,
                         size =>
                         {
                             DataFrame dataframe = frame;

                             if (size <= 0)
                             {
                                OnError();
                                return;
                             }

                             for (int i = 0; i < size; i++)
                                 _queue.Enqueue(buffer[i]);

                             while (_queue.Count > 0)
                             {
                                 dataframe.Append(_queue.Dequeue());
                                 if (!dataframe.IsComplete) continue;

                                 string data = dataframe.ToString();
                                 var copy = OnMessage;
                                 if(copy != null)
                                     copy(data);
                                 dataframe = new DataFrame();
                             }
                             Receive(dataframe);
                         },
                         e =>
                         {
                             FleckLog.Error("Recieve failed", e);
                             OnError();
                         });
        }
    }
}