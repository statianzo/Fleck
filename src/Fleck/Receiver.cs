using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fleck
{
    public class Receiver
    {
        private readonly ISocket _socket;
        private readonly Action<string> _messageAction;
        private readonly Action _closeAction;
        private const int BufferSize = 16384;
        private readonly Queue<byte> _queue;

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
            var segment = new ArraySegment<byte>(buffer);

            var task = Task<int>.Factory.FromAsync(Socket.BeginReceive, Socket.EndReceive, new[] { segment }, SocketFlags.None, null);
            task.ContinueWith(t =>
                {
                    int size = t.Result;
                    var dataframe = frame;

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

                        var data = dataframe.ToString();
                        _messageAction(data);
                        dataframe = new DataFrame();
                    }
                    Receive(dataframe);
                }, TaskContinuationOptions.NotOnFaulted);
            task.ContinueWith(t =>
                {
                    FleckLog.Error("Recieve failed", t.Exception);
                    _closeAction();
                }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}