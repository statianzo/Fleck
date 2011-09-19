using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Fleck.Interfaces;

namespace Fleck.ResponseBuilders
{
    public class Hybi14ResponseBuilder : IResponseBuilder
    {
        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public bool CanHandle(WebSocketHttpRequest request)
        {
            string version;
            request.Headers.TryGetValue("Sec-WebSocket-Version", out version);
            return "8".Equals(version) || "13".Equals(version);
        }

        public byte[] Build(WebSocketHttpRequest request)
        {
            FleckLog.Debug("Building Hybi-14 Response");
            
            var builder = new StringBuilder();

            builder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");

            var responseKey =  CreateResponseKey(request.Headers["Sec-WebSocket-Key"]);
            builder.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", responseKey);
            builder.Append("\r\n");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        public string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }
        
        public ISender CreateSender(ISocket socket)
        {
            return new Hybi14Sender(socket);
        }
        
        public IReceiver CreateReceiver(ISocket socket)
        {
            return new Hybi14Receiver(socket);
        }
    }
}