using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;

namespace Nugget
{
    public class ClientHandshake
    {
        public string Origin { get; set; }
        public string Host { get; set; }
        public string ResourcePath { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public ArraySegment<byte> ChallengeBytes { get; set; }
        public HttpCookieCollection Cookies { get; set; }
        public string SubProtocol { get; set; }
        public Dictionary<string,string> AdditionalFields { get; set; }

        /// <summary>
        /// Put together a string with the values of the handshake - excluding the challenge
        /// </summary>
        public override string ToString()
        {
            var stringShake = "GET " + ResourcePath + " HTTP/1.1\r\n" +
                              "Upgrade: WebSocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Origin: " + Origin + "\r\n" +
                              "Host: " + Host + "\r\n" +
                              "Sec-Websocket-Key1: " + Key1 + "\r\n" +
                              "Sec-Websocket-Key2: " + Key2 + "\r\n";


            if (Cookies != null)
            {
                stringShake += "Cookie: " + Cookies.ToString() + "\r\n";
            }
            if (SubProtocol != null)
                stringShake += "Sec-Websocket-Protocol: " + SubProtocol + "\r\n";

            if (AdditionalFields != null)
            {
                foreach (var field in AdditionalFields)
                {
                    stringShake += field.Key + ": " + field.Value + "\r\n";
                }
            }
            stringShake += "\r\n";

            return stringShake;
        }


    }

    public class ServerHandshake
    {
        public string Origin { get; set; }
        public string Location { get; set; }
        public byte[] AnswerBytes { get; set; }
        public string SubProtocol { get; set; }
        public Dictionary<string, string> AdditionalFields { get; set; }
    }
}
