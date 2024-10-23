using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Fleck.Handlers
{
    public static class Draft76Handler
    {
        private const byte End = 255;
        private const byte Start = 0;
        private const int MaxSize = 1024 * 1024 * 5;
                
        public static IHandler Create(WebSocketHttpRequest request, Action<string> onMessage)
        {
            return new ComposableHandler
            {
                TextFrame = Draft76Handler.FrameText,
                Handshake = sub => Draft76Handler.Handshake(request, sub),
                ReceiveData = data => ReceiveData(onMessage, data)
            };
        }

        public static void ReceiveData(Action<string> onMessage, List<byte> data)
        {
            while (data.Count > 0)
            {
                if (data[0] != Start)
                {
                    FleckLog.Error("Invalid frame start.");
                    throw new WebSocketException(WebSocketStatusCodes.InvalidFramePayloadData);
                }

                var endIndex = data.IndexOf(End);
                if (endIndex < 0)
                {
                    FleckLog.Warn("End marker not found, waiting for more data.");
                    return;
                }

                if (endIndex > MaxSize)
                {
                    FleckLog.Error("Message too big.");
                    throw new WebSocketException(WebSocketStatusCodes.MessageTooBig);
                }

                var bytes = data.Skip(1).Take(endIndex - 1).ToArray();
                data.RemoveRange(0, endIndex + 1);
                var message = Encoding.UTF8.GetString(bytes);
                onMessage(message);
            }
        }
        public static byte[] FrameText(string data)
        {
            //Utilizing ArrayPool<T> reduces heap allocation
            //by reusing arrays from a pool. This can improve performance
            //by reducing the amount and frequency
            //of garbage collections.
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var wrappedBytes = ArrayPool<byte>.Shared.Rent(bytes.Length + 2);

            try
            {
                wrappedBytes[0] = Start;
                wrappedBytes[wrappedBytes.Length - 1] = End;
                Array.Copy(bytes, 0, wrappedBytes, 1, bytes.Length);
                return wrappedBytes;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(wrappedBytes);
            }
        }
        
        public static byte[] Handshake(WebSocketHttpRequest request, string subProtocol)
        {
            FleckLog.Debug("Building Draft76 Response");
            
            var builder = new StringBuilder();
            builder.Append("HTTP/1.1 101 WebSocket Protocol Handshake\r\n");
            builder.Append("Upgrade: WebSocket\r\n");
            builder.Append("Connection: Upgrade\r\n");
            builder.AppendFormat("Sec-WebSocket-Origin: {0}\r\n",  request["Origin"]);
            builder.AppendFormat("Sec-WebSocket-Location: {0}://{1}{2}\r\n", request.Scheme, request["Host"], request.Path);

            if (subProtocol != null)
              builder.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", subProtocol);
                
            builder.Append("\r\n");
            
            var key1 = request["Sec-WebSocket-Key1"]; 
            var key2 = request["Sec-WebSocket-Key2"]; 
            var challenge = new ArraySegment<byte>(request.Bytes, request.Bytes.Length - 8, 8);
            
            var answerBytes = CalculateAnswerBytes(key1, key2, challenge);

            byte[] byteResponse = Encoding.ASCII.GetBytes(builder.ToString());
            int byteResponseLength = byteResponse.Length;
            Array.Resize(ref byteResponse, byteResponseLength + answerBytes.Length);
            Array.Copy(answerBytes, 0, byteResponse, byteResponseLength, answerBytes.Length);
            
            return byteResponse;
        }
        public static byte[] CalculateAnswerBytes(string key1, string key2, ArraySegment<byte> challenge)
        {
            byte[] result1Bytes = ParseKey(key1);
            byte[] result2Bytes = ParseKey(key2);

            var rawAnswer = new byte[16];
            Array.Copy(result1Bytes, 0, rawAnswer, 0, 4);
            Array.Copy(result2Bytes, 0, rawAnswer, 4, 4);
            Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);
            //Replaced MD5 with more secure SHA-256, since MD5 is considered cryptographically unsuitable
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(rawAnswer);
            }
        }
        private static byte[] ParseKey(string key)
        {
            int spaces = key.Count(x => x == ' ');
            var digits = new String(key.Where(Char.IsDigit).ToArray());

            var value = (Int32)(Int64.Parse(digits) / spaces);

            byte[] result = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);
            return result;
        }
    }
}
