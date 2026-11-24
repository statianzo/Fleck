using System;
using System.Linq;
using System.Text;

namespace Fleck
{
    public class RequestParser
    {
        private static readonly char[] LineBreaks = { '\r', '\n' };

        public static WebSocketHttpRequest Parse(ArraySegment<byte> bytes)
        {
            return Parse(bytes, "ws");
        }

        public static WebSocketHttpRequest Parse(ArraySegment<byte> bytes, string scheme)
        {
            if (bytes.Count > 4096)
                return null;

            // Check for websocket request header
            var body = Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
            var lines = body.Split(new[] { "\r\n" }, StringSplitOptions.None);

            var requestParts = lines[0].Split(' ');
            if (lines.Length < 2 || requestParts.Length != 3 || !IsToken(requestParts[0]) || !IsToken(requestParts[1]) || requestParts[2] != "HTTP/1.1")
                return null;

            var request = new WebSocketHttpRequest
            {
                Method = requestParts[0],
                Path = requestParts[1],
                Scheme = scheme
            };

            for (var i = 1; i < lines.Length - 1 && lines[i].Length > 0; i++)
            {
                var line = lines[i];
                var colon = line.IndexOf(':');
                if (colon <= 0 || line.IndexOfAny(LineBreaks) >= 0)
                    return null;

                request.Headers[line.Substring(0, colon)] = line.Substring(colon + 1).Trim();
            }

            return request.Headers.Count > 0 ? request : null;
        }

        private static bool IsToken(string value)
        {
            return value.Length > 0 && !value.Any(char.IsWhiteSpace);
        }
    }
}

