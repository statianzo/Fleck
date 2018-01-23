using System;
using System.Collections.Generic;
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
            var extract = ExtractRequest(body);

            if (extract == null)
            {
                // No websocket request header found, check for a flash socket policy request
                var match = _FlashSocketPolicyRequestRegex.Match(body);
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
                Method = extract.Method,
                Path = extract.Path,
                Body = extract.Body,
                Bytes = bytes,
                Scheme = scheme
            };

            for (var i = 0; i < extract.Headers.Count; i++)
            {
                request.Headers[extract.Headers[i].Name] = extract.Headers[i].Value;
            }
            return request;
        }

        private static ExtractRequestModel ExtractRequest(string request)
        {
            var lines = ReadFromRequest(request);
            //emtpy return null
            if (lines == null || lines.Count == 0) return null;
            //validate request line
            var validRequestLine = ValidateRequestLine(lines[0], out var path, out var method);
            if (validRequestLine == false) return null;
            //read headers and body
            var validHeadersAndBody = ValidHeaderAndBody(lines, out var headers, out var body);
            if (validHeadersAndBody == false) return null;
            var model = new ExtractRequestModel
            {
                Body = body,
                Headers = headers,
                Method = method,
                Path = path
            };
            return model;
        }

        private static bool ValidHeaderAndBody(IList<string> lines, out IList<RequestHeaderModel> headers, out string body)
        {
            headers = new List<RequestHeaderModel>();
            body = null;
            for (int i = 1; i < lines.Count; i++)
            {
                var headerLine = lines[i];
                if (string.IsNullOrEmpty(headerLine))
                {
                    //next line is last line
                    if (i + 2 == lines.Count)
                    {
                        body = lines[i + 1];
                    }
                    break;
                }
                //partial header should be null
                if (!headerLine.Contains(":")) return false;
                var splitHeader = headerLine.Split(new char[] { ':' }, 2);
                var headerName = string.Empty;
                var headerValue = string.Empty;
                if (splitHeader.Length == 1)
                {
                    headerName = splitHeader[0];
                }
                else
                {
                    headerName = splitHeader[0];
                    headerValue = splitHeader[1].TrimStart(' ');
                }
                headers.Add(new RequestHeaderModel(headerName, headerValue));
            }
            //no header request should be null
            if (headers.Count == 0) return false;
            return true;
        }

        private static bool ValidateRequestLine(string requestLine, out string path, out string method)
        {
            path = null;
            method = null;
            var lineContent = requestLine.Split(' ');
            if (lineContent.Length != 3) return false;
            if (lineContent[2] != "HTTP/1.1") return false;
            method = lineContent[0];
            path = lineContent[1];
            return true;
        }

        private static IList<string> ReadFromRequest(string request)
        {
            if (string.IsNullOrEmpty(request)) return null;
            var lines = new List<string>();
            var reader = new StringReader(request);
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;
                lines.Add(line);
            }
            return lines;
        }
    }

    public class ExtractRequestModel
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public IList<RequestHeaderModel> Headers { get; set; }

        public string Body { get; set; }
    }

    public class RequestHeaderModel
    {
        public RequestHeaderModel(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }
}

