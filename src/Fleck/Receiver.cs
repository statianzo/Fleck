using System;
using System.Collections.Generic;

namespace Fleck
{
    public class Receiver
    {
        private const int BufferSize = 16384;
        private readonly Action _closeAction;
        private readonly Action<string> _messageAction;
        private readonly Queue<byte> _queue;
        private readonly ISocket _socket;

        public Receiver(ISocket socket, Action<string> messageAction, Action closeAction)
        {
            _socket = socket;
            _messageAction = messageAction;
            _closeAction = closeAction;
            _queue = new Queue<byte>();
        }

        private ISocket Socket
        {
            get { return _socket; }
        }

        public void Receive(DataFrame frame = null)
        {
            if (frame == null)
                frame = new DataFrame();

            var buffer = new byte[BufferSize];

            if (Socket == null || !Socket.Connected)
            {
                _closeAction();
                return;
            }

            Socket
                .Receive(buffer,
                         size =>
                         {
                             DataFrame dataframe = frame;

                             if (size <= 0)
                             {
                                 _closeAction();
                                 return;
                             }

                             for (int i = 0; i < size; i++)
                                 _queue.Enqueue(buffer[i]);

                             while (_queue.Count > 0)
                             {
                                 dataframe.Append(_queue.Dequeue());
                                 if (!dataframe.IsComplete) continue;

                                 string data = dataframe.ToString();
                                 _messageAction(data);
                                 dataframe = new DataFrame();
                             }
                             Receive(dataframe);
                         },
                         e =>
                         {
                             FleckLog.Error("Recieve failed", e);
                             _closeAction();
                         });
        }
    }
}