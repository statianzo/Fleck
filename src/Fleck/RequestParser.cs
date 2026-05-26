using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Fleck
{
    public class RequestParser
    {
        // Pattern without body
        const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" +
                               @"((?<field_name>[^:\r\n]+):\s*(?<field_value>[^\r\n]*)\r\n)+";

        // Timeout setted
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        // Added timeout
        private static readonly Regex _regex = new Regex(
            pattern, 
            RegexOptions.IgnoreCase | RegexOptions.Compiled, 
            RegexTimeout);

        public static WebSocketHttpRequest Parse(byte[] bytes)
        {
            return Parse(bytes, "ws");
        }

        public static WebSocketHttpRequest Parse(byte[] bytes, string scheme)
        {
            // Try Catch for timeout
            try
            {
                var text = Encoding.UTF8.GetString(bytes);
                Match match = _regex.Match(text);

                if (!match.Success)
                    return null;

                var request = new WebSocketHttpRequest
                {
                    Method = match.Groups["method"].Value,
                    Path = match.Groups["path"].Value,
                    Bytes = bytes,
                    Scheme = scheme
                };

                var fields = match.Groups["field_name"].Captures;
                var values = match.Groups["field_value"].Captures;
                for (var i = 0; i < fields.Count; i++)
                {
                    request.Headers[fields[i].Value] = values[i].Value;
                }

                // Added the Body extraction
                int headerEndIndex = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEndIndex != -1 && text.Length > headerEndIndex + 4)
                {
                    request.Body = text.Substring(headerEndIndex + 4);
                }

                return request;
            }
            catch (RegexMatchTimeoutException)
            {
                return null; 
            }
        }
    }
}
