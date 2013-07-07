using System;
using System.Linq;

namespace Fleck
{
    public static class SubProtocolNegotiator
    {
        public static string Negotiate(string[] server, string[] client)
        {
            if (!server.Any() || !client.Any()) {
                return null;
            }

            var matches = client.Intersect(server);
            if (!matches.Any()) {
                throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
            }
            return matches.First();
        }
    }
}
