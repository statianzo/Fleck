﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace Fleck
{
    public class WebSocketConnectionInfo : IWebSocketConnectionInfo
    {
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
                               NegotiatedSubProtocol = negotiatedSubprotocol
                           };
            var cookieHeader = request["Cookie"];

            if (cookieHeader != null)
            {
                var cookies = cookieHeader.Split(';');
                foreach (var cookie in cookies)
                {
                    var parts = cookie.Split('=');
                    if (parts.Length == 2)
                    {
                        info.Cookies.Add(parts[0], parts[1]);
                    }
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
    }
}
