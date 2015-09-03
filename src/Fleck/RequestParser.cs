using System.Text;
using System.Text.RegularExpressions;

namespace Fleck
{
    public class RequestParser
    {
        const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
                               @"((?<field_name>[^:\r\n]+):(?([^\r\n])\s)*(?<field_value>[^\r\n]*)\r\n)+" + //headers
                               @"\r\n" + //newline
                               @"(?<body>.+)?";
        const string FlashSocketPolicyRequestPattern = @"^[<]policy-file-request\s*[/][>]";

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _FlashSocketPolicyRequestRegex = new Regex(FlashSocketPolicyRequestPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static WebSocketHttpRequest Parse(byte[] bytes)
        {
            return Parse(bytes, "ws");
        }

        public static WebSocketHttpRequest Parse(byte[] bytes, string scheme)
        {
            // Check for websocket request header
            var body = Encoding.UTF8.GetString(bytes);
            Match match = _regex.Match(body);

            if (!match.Success)
            {
                // No websocket request header found, check for a flash socket policy request
                match = _FlashSocketPolicyRequestRegex.Match(body);
                if (match.Success)
                {
                    // It's a flash socket policy request, so return
                    return new WebSocketHttpRequest
                    {
                        Body = body,
                        Bytes = bytes
                    };
                }
                else
                {
                    return null;
                }
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

