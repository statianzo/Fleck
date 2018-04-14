using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fleck
{
    public class WebSocketConnectionInfo : IWebSocketConnectionInfo
    {
        const string CookiePattern = @"((;)*(\s)*(?<cookie_name>[^=]+)=(?<cookie_value>[^\;]+))+";
        private static readonly Regex CookieRegex = new Regex(CookiePattern, RegexOptions.Compiled);

        public static WebSocketConnectionInfo Create(WebSocketHttpRequest request, string clientIp, int clientPort, string negotiatedSubprotocol)
        {
            var info = new WebSocketConnectionInfo
                           {
                               Origin = request["Origin"] ?? request["Sec-WebSocket-Origin"],
                               Host = request["Host"],
                               SubProtocol = request["Sec-WebSocket-Protocol"],
                               Path = request.Path,
                               ClientIpAddress = clientIp,
                               ClientPort = clientPort,
                               NegotiatedSubProtocol = negotiatedSubprotocol,
                               Headers = new Dictionary<string, string>(request.Headers, System.StringComparer.InvariantCultureIgnoreCase)
                           };
            var cookieHeader = request["Cookie"];

            if (cookieHeader != null)
            {
                var match = CookieRegex.Match(cookieHeader);
                var fields = match.Groups["cookie_name"].Captures;
                var values = match.Groups["cookie_value"].Captures;
                for (var i = 0; i < fields.Count; i++)
                {
                    var name = fields[i].ToString();
                    var value = values[i].ToString();
                    info.Cookies[name] = value;
                }
            }

            return info;
        }


        WebSocketConnectionInfo()
        {
            Cookies = new Dictionary<string, string>();
            Id = Guid.NewGuid();
        }

        public string NegotiatedSubProtocol { get; private set; }
        public string SubProtocol { get; private set; }
        public string Origin { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }
        public string ClientIpAddress { get; set; }
        public int    ClientPort { get; set; }
        public Guid Id { get; set; }

        public IDictionary<string, string> Cookies { get; private set; }
        public IDictionary<string, string> Headers { get; private set; }
    }
}
