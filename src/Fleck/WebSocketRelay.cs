using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fleck
{
    public class WebSocketRelay: List<IWebSocketConnection>
    {
        public string Name { get; set; }
        public Guid ID { get; set; }
        public long MaxLength { get; set; }
        public static List<WebSocketRelay> Relays { get; set; }

        public WebSocketRelay()
        {
            this.ID = Guid.NewGuid();
            this.Name = this.ID.ToString().Substring(0, 6).ToUpper();
            this.MaxLength = Int64.MaxValue;
        }

        public WebSocketRelay(Guid id, string name, int maxLength)
        {
            this.ID = id;
            this.Name = name;
            this.MaxLength = maxLength;
        }

        public bool IsFull()
        {
            return this.LongCount() == this.MaxLength;
        }

        public bool IsEmpty()
        {
            return this.Count == 0;
        }

        public bool Contains(WebSocketConnection socket)
        {
            if (socket == null || socket.ConnectionInfo == null) return false;
            return Contains(socket.ConnectionInfo.Id);
        }

        public bool Contains(Guid id)
        {
            for (int index = 0; index <= this.Count - 1; index++)
            {
                if (id.Equals(this[index].ConnectionInfo.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<IWebSocketConnection> Without(IWebSocketConnection ws)
        {
            if (ws == null || ws.ConnectionInfo == null) return this.AsEnumerable();
            return this.Where((socket) => socket.ConnectionInfo.Id != ws.ConnectionInfo.Id);
        }

        public IWebSocketConnection GetSocket(Guid id)
        {
            return (from socket in this where socket.ConnectionInfo.Id.Equals(id) select socket).FirstOrDefault();
        }

        private void _broadcast(string Message, IEnumerable<IWebSocketConnection> recipients)
        {
            foreach (var socket in recipients)
            {
                socket.Send(Message);
            }
        }

        private void _broadcast(byte[] Message, IEnumerable<IWebSocketConnection> recipients)
        {
            foreach (var socket in recipients)
            {
                socket.Send(Message);
            }
        }

        private IEnumerable<IWebSocketConnection> _validateBroadcast(Guid Sender, bool IncludeSender)
        {
            if (this.Count == 0 | !this.Contains(Sender))
            {
                return null;
            }
            var senderSocket = GetSocket(Sender);
            if (senderSocket == null) return null;
            IEnumerable<IWebSocketConnection> query = this;
            if (IncludeSender == false) query = this.Without(senderSocket);
            return query;
        }

        private IEnumerable<IWebSocketConnection> _validateBroadcast(Guid Sender, Func<IWebSocketConnection, bool> recipientsFilter)
        {
            if (this.Count == 0 | !this.Contains(Sender))
            {
                return null;
            }
            var senderSocket = GetSocket(Sender);
            if (senderSocket == null) return null;
            IEnumerable<IWebSocketConnection> query = this.Where(recipientsFilter);
            return query;
        }

        public bool Broadcast(string Message, Guid Sender, bool IncludeSender = false)
        {
            IEnumerable<IWebSocketConnection> recipients = _validateBroadcast(Sender, IncludeSender);
            if (recipients == null) return false;
            else
            {
                _broadcast(Message, recipients);
            }
            return true;
        }

        public bool Broadcast(byte[] Message, Guid Sender, bool IncludeSender = false)
        {
            IEnumerable<IWebSocketConnection> recipients = _validateBroadcast(Sender, IncludeSender);
            if (recipients == null) return false;
            else
            {
                _broadcast(Message, recipients);
            }
            return true;
        }

        public bool Broadcast(string Message, Guid Sender, Func<IWebSocketConnection, bool> recipientsFilter)
        {
            IEnumerable<IWebSocketConnection> recipients = _validateBroadcast(Sender, recipientsFilter);
            if (recipients == null) return false;
            else
            {
                _broadcast(Message, recipients);
            }
            return true;
        }

        public bool Broadcast(byte[] Message, Guid Sender, Func<IWebSocketConnection, bool> recipientsFilter)
        {
            IEnumerable<IWebSocketConnection> recipients = _validateBroadcast(Sender, recipientsFilter);
            if (recipients == null) return false;
            else
            {
                _broadcast(Message, recipients);
            }
            return true;
        }

        public new void Add(IWebSocketConnection ws)
        {
            if (!(this.Count + 1 > this.MaxLength))
            {
                if (ws != null && ws.ConnectionInfo != null)
                {
                    if (!Contains(ws)) base.Add(ws);
                }
                else base.Add(ws);
            }
        }
    }
}
