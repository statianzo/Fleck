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
            var builder = new StringBuilder();

            builder.AppendLine("HTTP/1.1 101 Switching Protocols");
            builder.AppendLine("Upgrade: websocket");
            builder.AppendLine("Connection: Upgrade");
            var headers = new Dictionary<string, string>();
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            var responseKey =  CreateResponseKey(request.Headers["Sec-WebSocket-Key"]);
            builder.AppendLine("Sec-WebSocket-Accept: " + responseKey);
            builder.AppendLine();

            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        public string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }
    }
}