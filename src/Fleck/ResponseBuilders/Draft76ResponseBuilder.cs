using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Fleck.Interfaces;
using System.Security.Cryptography;
using System.Linq;

namespace Fleck.ResponseBuilders
{
    public class Draft76ResponseBuilder : IResponseBuilder
    {
        private readonly string _location;
        private readonly string _scheme;
        private readonly string _origin;
        
        public Draft76ResponseBuilder(string location, string scheme, string origin)
        {
            _location = location;
            _scheme = scheme;
            _origin = origin;
        }

        public bool CanHandle(WebSocketHttpRequest request)
        {
            return request.Headers.ContainsKey("Sec-WebSocket-Key1") && request.Headers.ContainsKey("Sec-WebSocket-Key2");
        }

        public byte[] Build(WebSocketHttpRequest request)
        {
            var clientHandshake = ParseClientHandshake(request);
            
            if (clientHandshake.Validate(_origin, _location, _scheme)) {
                FleckLog.Debug("Client handshake validated");
                ServerHandshake serverShake = GenerateResponseHandshake(clientHandshake);
                
                string stringShake = serverShake.ToResponseString();

                byte[] byteResponse = Encoding.UTF8.GetBytes(stringShake);
                int byteResponseLength = byteResponse.Length;
                Array.Resize(ref byteResponse, byteResponseLength + serverShake.AnswerBytes.Length);
                Array.Copy(serverShake.AnswerBytes, 0, byteResponse, byteResponseLength, serverShake.AnswerBytes.Length);
            
                return byteResponse;

            } else {
                FleckLog.Info("Client handshake failed to validate");
                return new byte[0];
            }
        }
        
        public static ClientHandshake ParseClientHandshake(WebSocketHttpRequest request)
        {
            Func<string, string> getHeader = key => {
                string result;
                request.Headers.TryGetValue(key, out result);
                return result;
            };
            var challenge = request.Bytes.Skip(request.Bytes.Length - 8).ToArray();
            var handshake = new ClientHandshake
            {
                ChallengeBytes = new ArraySegment<byte>(challenge),
                Key1 = getHeader("Sec-WebSocket-Key1"),
                Key2 = getHeader("Sec-WebSocket-Key2"),
                SubProtocol = getHeader("Sec-WebSocket-Protocol"),
                Origin = getHeader("Origin"),
                Cookies = getHeader("Cookie"),
                Host = getHeader("Host"),
                ResourcePath = request.Path
            };
            
            return handshake;
        }

        public ServerHandshake GenerateResponseHandshake(ClientHandshake clientHandshake)
        {
            var responseHandshake = new ServerHandshake
            {
                Location = string.Format("{0}://{1}{2}", _scheme, clientHandshake.Host, clientHandshake.ResourcePath),
                Origin = clientHandshake.Origin,
                SubProtocol = clientHandshake.SubProtocol
            };

            var challenge = new byte[8];
            Array.Copy(clientHandshake.ChallengeBytes.Array, clientHandshake.ChallengeBytes.Offset, challenge, 0, 8);

            responseHandshake.AnswerBytes =
                CalculateAnswerBytes(clientHandshake.Key1, clientHandshake.Key2, clientHandshake.ChallengeBytes);

            return responseHandshake;
        }
        
        public static byte[] CalculateAnswerBytes(string key1, string key2, ArraySegment<byte> challenge)
        {
            byte[] result1Bytes = ParseKey(key1);
            byte[] result2Bytes = ParseKey(key2);

            var rawAnswer = new byte[16];
            Array.Copy(result1Bytes, 0, rawAnswer, 0, 4);
            Array.Copy(result2Bytes, 0, rawAnswer, 4, 4);
            Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);

            MD5 md5 = MD5.Create();
            return md5.ComputeHash(rawAnswer);
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
}

