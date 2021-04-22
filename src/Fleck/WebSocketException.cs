using System;
namespace Fleck
{
    public class WebSocketException : Exception
    {
        public WebSocketException(ushort statusCode) : this(statusCode, PrepareExceptionMessage(statusCode))
        {
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

        private static string PrepareExceptionMessage(ushort statusCode)
        {
            return $"Exception with status code {statusCode}: {WebSocketStatusCodes.ConvertStatusCodeToString(statusCode)}";
        }
    }
}
