using System;
using System.IO;
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
            var cookie = SplitCookieWithBody(ref body);
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
            if (cookie != null)
            {
                request.Headers["Cookie"] = cookie;
            }
            return request;
        }

        /// <summary>
        /// In order to prevent the cookie is too large and effect the regex match time
        /// split out the cookie in header to speed up the match time
        /// </summary>
        /// <param name="body">request body</param>
        /// <returns>cookie</returns>
        private static string SplitCookieWithBody(ref string body)
        {
            string cookie = null;
            string lastLine = null;
            var reader = new StringReader(body);
            var builder = new StringBuilder();
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;
                if (line.StartsWith("Cookie:"))
                {
                    cookie = line.Substring(8);
                    continue;
                }
                builder.AppendLine(line);
                lastLine = line;
            }
            body = builder.ToString();
            if (!string.IsNullOrEmpty(lastLine))
            {
                body = body.Remove(body.LastIndexOf("\r"));
            }
            return cookie;
        }
    }
}

