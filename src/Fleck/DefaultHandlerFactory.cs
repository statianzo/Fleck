using System;
using Fleck.Interfaces;

namespace Fleck
{
    public class DefaultHandlerFactory : IHandlerFactory
    {
        private string _scheme;
        public DefaultHandlerFactory(string scheme)
        {
            RequestParser = new RequestParser();
            _scheme = scheme;
        }
        
        public IRequestParser RequestParser { get; set; }

        public IHandler BuildHandler(byte[] data, Action<string> onMessage, Action onClose)
        {
            if (!RequestParser.IsComplete(data))
                return null;
            
            var request = RequestParser.Parse(data, _scheme);
            
            var version = GetVersion(request);
            
            switch (version)
            {
                case "76":
                    return Draft76Handler.Create(request, onMessage);
                case "8":
                    return Hybi13Handler.Create(request, onMessage, onClose);
            }
            
            throw new WebSocketException("Unsupported Request");
        }
        
        public static string GetVersion(WebSocketHttpRequest request) 
        {
            string version;
            if (request.Headers.TryGetValue("Sec-WebSocket-Version", out version))
                return version;
                
            if (request.Headers.TryGetValue("Sec-WebSocket-Draft", out version))
                return version;
            
            if (request.Headers.ContainsKey("Sec-WebSocket-Key1"))
                return "76";
            
            return "75";
        }
    }
}

