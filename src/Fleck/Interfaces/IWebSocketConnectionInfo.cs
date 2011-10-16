using System.Collections.Generic;

namespace Fleck
{
    public interface IWebSocketConnectionInfo
    {
        string SubProtocol { get; }
        string Origin { get; }
        string Host { get; }
        IDictionary<string, string> Cookies { get; }
    }
}
