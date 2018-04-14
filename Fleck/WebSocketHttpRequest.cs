using System.Collections.Generic;
using System;

namespace Fleck
{
    public class WebSocketHttpRequest
    {
        private readonly IDictionary<string, string> _headers = new Dictionary<string, string>(System.StringComparer.InvariantCultureIgnoreCase);

        public string Method { get; set; }

        public string Path { get; set; }

        public string Body { get; set; }

        public string Scheme { get; set; }

        public byte[] Bytes { get; set; }

        public string this[string name]
        {
            get
            {
                string value;
                return _headers.TryGetValue(name, out value) ? value : default(string);
            }
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                return _headers;
            }
        }
        
        public string[] SubProtocols {
          get
          {
            string value;
          return _headers.TryGetValue("Sec-WebSocket-Protocol", out value)
              ? value.Split(new []{',', ' '}, StringSplitOptions.RemoveEmptyEntries)
              : new string[0];
          }
        }
    }
}

