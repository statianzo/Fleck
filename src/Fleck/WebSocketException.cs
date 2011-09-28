using System;
namespace Fleck
{
    public class WebSocketException : Exception
    {
        public WebSocketException() : base() { }
        
        public WebSocketException(string message) : base(message) {}
        
        public WebSocketException(string message, Exception innerException) : base(message, innerException) {}
    }
}
