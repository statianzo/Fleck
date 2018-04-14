using System.Collections.Generic;
using System;

namespace Fleck
{
    public interface IWebSocketConnectionInfo
    {
        string SubProtocol { get; }
        string Origin { get; }
        string Host { get; }
        string Path { get; }
        string ClientIpAddress { get; }
        int    ClientPort { get; }
        IDictionary<string, string> Cookies { get; }
        IDictionary<string, string> Headers { get; }
        Guid Id { get; }
        string NegotiatedSubProtocol { get; }
    }
}
