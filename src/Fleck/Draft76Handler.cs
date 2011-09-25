using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace Fleck
{
    public class Draft76Handler
    {
    
        const byte End = 255;
        const byte Start = 0;
        const int MaxSize = 1024 * 1024 * 5;
        
        public static IHandler Create(WebSocketHttpRequest request, Action<string> onMessage)
        {
            return new ComposableHandler
            {
                Frame = Draft76Handler.FrameText,
                Handshake = () => Draft76Handler.Handshake(request)
            };
        }
        
        public static void RecieveData(Action<string> onMessage, List<byte> data)
        {
            while (data.Count > 0)
            {
                if (data[0] != Start)
                    throw new WebSocketException("Invalid Frame");
                
                var endIndex = data.IndexOf(End);
                if (endIndex < 0)
                    return;
                
                if (endIndex > MaxSize)
                    throw new WebSocketException("Frame too large");
                
                var bytes = data.Skip(1).Take(endIndex - 2).ToArray();
                
                data.RemoveRange(0, endIndex + 1);
                
                var message = Encoding.UTF8.GetString(bytes);
                
                onMessage(message);
            }
        }
        
        public static byte[] FrameText(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            // wrap the array with the wrapper bytes
            var wrappedBytes = new byte[bytes.Length + 2];
            wrappedBytes[0] = Start;
            wrappedBytes[wrappedBytes.Length - 1] = End;
            Array.Copy(bytes, 0, wrappedBytes, 1, bytes.Length);
            return wrappedBytes;
        }
        
        public static byte[] Handshake(WebSocketHttpRequest request)
        {
            FleckLog.Debug("Building Draft76 Response");
            
            var builder = new StringBuilder();
            builder.Append("HTTP/1.1 101 WebSocket Protocol Handshake\r\n");
            builder.Append("Upgrade: WebSocket\r\n");
            builder.Append("Connection: Upgrade\r\n");
            builder.AppendFormat("Sec-WebSocket-Origin: {0}\r\n",  request["Origin"]);
            builder.AppendFormat("Sec-WebSocket-Location: {0}\r\n", request["Location"]);

            if (request.Headers.ContainsKey("Sec-WebSocket-Protocol"))
                builder.AppendFormat("Sec-WebSocket-Protocol: {0}", request["Sec-WebSocket-Protocol"]);
                
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
        
        
        private static readonly MD5 Md5 = MD5.Create();
        
        public static byte[] CalculateAnswerBytes(string key1, string key2, ArraySegment<byte> challenge)
        {
            byte[] result1Bytes = ParseKey(key1);
            byte[] result2Bytes = ParseKey(key2);

            var rawAnswer = new byte[16];
            Array.Copy(result1Bytes, 0, rawAnswer, 0, 4);
            Array.Copy(result2Bytes, 0, rawAnswer, 4, 4);
            Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);

            return Md5.ComputeHash(rawAnswer);
        }

        public static byte[] ParseKey(string key)
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
    
    public class ComposableHandler : IHandler
    {
        public Func<byte[]> Handshake = () => new byte[0];
        public Func<string, byte[]> Frame = x => new byte[0];
        public Action<List<byte>> RecieveData = delegate { };
        public Func<int, byte[]> Close = i => new byte[0];
        
        private List<byte> _data = new List<byte>();

        public byte[] CreateHandshake()
        {
            return Handshake();
        }

        public void Recieve(IEnumerable<byte> data)
        {
            _data.AddRange(data);
            
            RecieveData(_data);
        }
        
        public byte[] FrameText(string text)
        {
            return Frame(text);
        }
        
        public byte[] FrameClose(int code)
        {
            return Close(code);
        }
    }
}
