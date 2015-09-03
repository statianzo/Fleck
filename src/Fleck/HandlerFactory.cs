using System;
using Fleck.Handlers;

namespace Fleck
{
    public class HandlerFactory
    {
        public static IHandler BuildHandler(WebSocketHttpRequest request, Action<string> onMessage, Action onClose, Action<byte[]> onBinary, Action<byte[]> onPing, Action<byte[]> onPong)
        {
            var version = GetVersion(request);
            
            switch (version)
            {
                case "76":
                    return Draft76Handler.Create(request, onMessage);
                case "7":
                case "8":
                case "13":
                    return Hybi13Handler.Create(request, onMessage, onClose, onBinary, onPing, onPong);
                case "policy-file-request":
                    return FlashSocketPolicyRequestHandler.Create(request);
            }
            
            throw new WebSocketException(WebSocketStatusCodes.UnsupportedDataType);
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
            
            if ((request.Body != null) && request.Body.ToLower().Contains("policy-file-request"))
                return "policy-file-request";

            return "75";
        }
    }
}

