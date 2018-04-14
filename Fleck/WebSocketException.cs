using System;
namespace Fleck
{
    public class WebSocketException : Exception
    {
        public WebSocketException(ushort statusCode) : base()
        {
            StatusCode = statusCode;
        }
        
        public WebSocketException(ushort statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
        
        public WebSocketException(ushort statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
        
        public ushort StatusCode { get; private set;}
    }
}
