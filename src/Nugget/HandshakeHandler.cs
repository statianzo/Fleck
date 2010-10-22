using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;
using System.Web;


namespace Nugget
{
    /// <summary>
    /// Handles the handshaking between the client and the host, when a new connection is created
    /// </summary>
    class HandshakeHandler
    {
        public string Origin { get; set; }
        public string Location { get; set; }
        public ClientHandshake ClientHandshake { get; set; }
        public Action<ClientHandshake> OnSuccess { get; set; }
                
        public HandshakeHandler(string origin, string location)
        {
            Origin = origin;
            Location = location;
        }

        class HandShakeState
        {
            public Socket socket;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
        }

   
        /// <summary>
        /// Shake hands with the connecting socket
        /// </summary>
        /// <param name="socket">The socket to send the handshake to</param>
        /// <param name="callback">a callback function that is called when the send has completed</param>
        public void Shake(Socket socket)
        {
            try
            {
                // create the state object, and save the relavent information.
                HandShakeState state = new HandShakeState();
                state.socket = socket;
                // receive the client handshake
                state.socket.BeginReceive(state.buffer, 0, HandShakeState.BufferSize, 0, new AsyncCallback(DoShake), state);
                
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown from method Receive:\n" + e.Message);
            }
        }

        private void DoShake(IAsyncResult ar)
        {
            var state = (HandShakeState)ar.AsyncState;
            int receivedByteCount = state.socket.EndReceive(ar);

            // parse the client handshake and generate a response handshake
            ClientHandshake = ParseClientHandshake(new ArraySegment<byte>(state.buffer, 0, receivedByteCount));

            var hasRequiredFields = (ClientHandshake.ChallengeBytes != null) &&
                                    (ClientHandshake.Host != null) &&
                                    (ClientHandshake.Key1 != null) &&
                                    (ClientHandshake.Key2 != null) &&
                                    (ClientHandshake.Origin != null) &&
                                    (ClientHandshake.ResourcePath != null);
            
            // check if the information in the client handshake is valid
            if (hasRequiredFields && "ws://"+ClientHandshake.Host == Location && ClientHandshake.Origin == Origin)
            {
                // generate a response for the client
                var serverShake = GenerateResponseHandshake();
                // send the handshake to the client
                BeginSendServerHandshake(serverShake, state.socket);
            }
            else
            {
                // the client shake isn't valid
                Log.Debug("invalid handshake received from "+state.socket.LocalEndPoint);
                state.socket.Close();
                return;
            }
        }

        private ClientHandshake ParseClientHandshake(ArraySegment<byte> byteShake)
        {
            // the "grammar" of the handshake
            var pattern = @"^(?<connect>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" +  // request line
                          @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+"; // unordered set of fields (name-chars colon space any-chars cr lf)

            // subtract the challenge bytes from the handshake
            var handshake = new ClientHandshake();
            ArraySegment<byte> challenge = new ArraySegment<byte>(byteShake.Array, byteShake.Count - 8, 8); // -8 : eight byte challenge
            handshake.ChallengeBytes = challenge;

            // get the rest of the handshake
            var utf8_handshake = Encoding.UTF8.GetString(byteShake.Array, 0, byteShake.Count - 8);

            // match the handshake against the "grammar"
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(utf8_handshake);
            var fields = match.Groups;

            // save the request path
            handshake.ResourcePath = fields["path"].Value;

            // run through every match and save them in the handshake object
            for (int i = 0; i < fields["field_name"].Captures.Count; i++)
            {
                var name = fields["field_name"].Captures[i].ToString();
                var value = fields["field_value"].Captures[i].ToString();

                switch (name.ToLower())
                {
                    case "sec-websocket-key1":
                        handshake.Key1 = value;
                        break;
                    case "sec-websocket-key2":
                        handshake.Key2 = value;
                        break;
                    case "sec-websocket-protocol":
                        handshake.SubProtocol = value;
                        break;
                    case "origin":
                        handshake.Origin = value;
                        break;
                    case "host":
                        handshake.Host = value;
                        break;
                    case "cookie":
                        // create and fill a cookie collection from the data in the handshake
                        handshake.Cookies = new HttpCookieCollection();
                        var cookies = value.Split(';');
                        foreach (var item in cookies)
                        {
                            // the name if before the '=' char
                            var c_name = item.Remove(item.IndexOf('='));
                            // the value is after
                            var c_value = item.Substring(item.IndexOf('=') + 1);
                            // put the cookie in the collection (this also parses the sub-values and such)
                            handshake.Cookies.Add(new HttpCookie(c_name.TrimStart(), c_value));
                        }
                        break;
                    default:
                        // some field that we don't know about
                        if (handshake.AdditionalFields == null)
                            handshake.AdditionalFields = new Dictionary<string, string>();
                        handshake.AdditionalFields[name] = value;
                        break;
                }
            }
            return handshake;
        }

        private ServerHandshake GenerateResponseHandshake()
        {
            var responseHandshake = new ServerHandshake();
            responseHandshake.Location = "ws://" + ClientHandshake.Host + ClientHandshake.ResourcePath;
            responseHandshake.Origin = ClientHandshake.Origin;
            responseHandshake.SubProtocol = ClientHandshake.SubProtocol;

            var challenge = new byte[8];
            Array.Copy(ClientHandshake.ChallengeBytes.Array, ClientHandshake.ChallengeBytes.Offset, challenge, 0, 8);
            
            byte[] bytes;
            CalculateAnswerBytes(out bytes, ClientHandshake.Key1, ClientHandshake.Key2, ClientHandshake.ChallengeBytes);
            responseHandshake.AnswerBytes = bytes;

            return responseHandshake;
        }
        
        private void BeginSendServerHandshake(ServerHandshake handshake, Socket socket)
        {
            var stringShake = "HTTP/1.1 101 Web Socket Protocol Handshake\r\n" +
                              "Upgrade: WebSocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Origin: " + handshake.Origin + "\r\n" +
                              "Sec-WebSocket-Location: " + handshake.Location + "\r\n";

            if (handshake.SubProtocol != null)
            {
                stringShake += "Sec-WebSocket-Protocol: " + handshake.SubProtocol + "\r\n";
            }
            stringShake += "\r\n";

            

            // generate a byte array representation of the handshake including the answer to the challenge
            byte[] byteResponse = Encoding.ASCII.GetBytes(stringShake);
            int byteResponseLength = byteResponse.Length;
            Array.Resize(ref byteResponse, byteResponseLength + handshake.AnswerBytes.Length);
            Array.Copy(handshake.AnswerBytes, 0, byteResponse, byteResponseLength, handshake.AnswerBytes.Length);

            socket.BeginSend(byteResponse, 0, byteResponse.Length, 0, EndSendServerHandshake, socket);
        }

        private void EndSendServerHandshake(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndSend(ar);

            if (OnSuccess != null)
            {
                OnSuccess(ClientHandshake);
            }
        }
        
        private void CalculateAnswerBytes(out byte[] answer, string key1, string key2, ArraySegment<byte> challenge)
        {
            // the following code is to conform with the protocol

            //  count the spaces
            int spaces1 = key1.Count(x => x == ' ');
            int spaces2 = key2.Count(x => x == ' ');

            // concat the digits
            var digits1 = new String(key1.Where(x => Char.IsDigit(x)).ToArray());
            var digits2 = new String(key2.Where(x => Char.IsDigit(x)).ToArray());

            // divide the digits with the number of spaces
            Int32 result1 = (Int32)(Int64.Parse(digits1) / spaces1);
            Int32 result2 = (Int32)(Int64.Parse(digits2) / spaces2);

            // convert the results to 32 bit big endian byte arrays
            byte[] result1bytes = BitConverter.GetBytes(result1);
            byte[] result2bytes = BitConverter.GetBytes(result2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(result1bytes);
                Array.Reverse(result2bytes);
            }

            // concat the two integers and the 8 challenge bytes from the client
            byte[] rawAnswer = new byte[16];
            Array.Copy(result1bytes, 0, rawAnswer, 0, 4);
            Array.Copy(result2bytes, 0, rawAnswer, 4, 4);
            Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);

            // compute the md5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            answer = md5.ComputeHash(rawAnswer);
        }
        
    }
}
