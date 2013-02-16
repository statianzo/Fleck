using System.Text;
using System.Text.RegularExpressions;

namespace Fleck
{
    public class RequestParser
    {
        // mjb
        //const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
        //                       @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+" + //headers
        //                       @"\r\n" + //newline
        //                       @"(?<body>.+)?";
        const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
                               @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+"; //headers

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static WebSocketHttpRequest Parse(byte[] bytes)
        {
            // mjb 
            //return Parse(bytes, "ws");
            return Parse(bytes, "ws", null, null);
        }

        // mjb public static WebSocketHttpRequest Parse(byte[] bytes, string scheme)
        public static WebSocketHttpRequest Parse(byte[] bytes, string scheme, AccessPolicyServer AccessPolicyServer, ISocket clientSocket)
        {
            var body = Encoding.UTF8.GetString(bytes);
            Match match = _regex.Match(body);

            // mjb
            //if (!match.Success)
            //    return null;
            if (!match.Success)
            {
                if (body == "<policy-file-request/>\0")
                {
                    if (AccessPolicyServer != null)
                    {
                        AccessPolicyServer.SendResponse(clientSocket.Socket);
                    }
                }

                return null;
            }

            var request = new WebSocketHttpRequest
            {
                Method = match.Groups["method"].Value,
                Path = match.Groups["path"].Value,
                Body = match.Groups["body"].Value,
                Bytes = bytes,
                Scheme = scheme
            };

            var fields = match.Groups["field_name"].Captures;
            var values = match.Groups["field_value"].Captures;
            for (var i = 0; i < fields.Count; i++)
            {
                var name = fields[i].ToString();
                var value = values[i].ToString();
                request.Headers[name] = value;
            }

            return request;
        }
    }
}

