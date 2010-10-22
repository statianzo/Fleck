using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Nugget.Tests
{
    class ClientSocket
    {
        public Socket Socket { get; set; }
        public ExtendedClientHandshake Handshake { get; set; }
        public Action<string> OnReceive { get; set; }

        public ClientSocket(ExtendedClientHandshake hs)
        {
            Handshake = hs;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            var tmp = Handshake.Host.Split(':');
            var port = 81;
            var host = Handshake.Host;
            if (tmp.Length > 1)
            {
                port = Int32.Parse(tmp[1]);
                host = tmp[0];
            }
            
            Socket.Connect(host, port);
            SendHandshake();
            if (!ReadHandshake())
            {
                Socket.Close();
            }
        }

        public string Receive(DataFrame frame = null)
        {

            if (frame == null)
                frame = new DataFrame();

            var buffer = new byte[256];

            int sizeOfReceivedData =  Socket.Receive(buffer);
            frame.Append(buffer);
            if (frame.IsComplete)
                return frame.ToString();
            else
                return Receive(frame);
        }

        public void Send(string data)
        {
            Socket.Send(DataFrame.Wrap(data));
        }

        public void SendAsync(string data)
        {
            Socket.AsyncSend(Encoding.UTF8.GetBytes(data));
        }

        public void ReceiveAsync(Action<string> callback, DataFrame frame = null)
        {
            var buffer = new byte[256];
            if (frame == null)
                frame = new DataFrame();

            Socket.AsyncReceive(buffer, frame, (sizeOfReceivedData, df) =>
            {
                var dataframe = (DataFrame)df;

                if (sizeOfReceivedData > 0)
                {
                    dataframe.Append(buffer);

                    if (dataframe.IsComplete)
                    {
                        var data = dataframe.ToString();

                        callback(data);

                    }
                    else // end is not is this buffer
                    {
                        ReceiveAsync(callback, dataframe); // continue to read
                    }
                }
            });
        }

        private bool ReadHandshake()
        {
 	        var hs = new byte[1024];
            int count = Socket.Receive(hs);

            var bytes = new ArraySegment<byte>(hs, 0, count);

            ServerHandshake handshake = null;
            if (bytes.Count > 0)
            {
                handshake = ParseServerHandshake(bytes);
            }


            var isValid = (handshake != null) &&
                        (handshake.Origin == Handshake.Origin) &&
                        (handshake.Location == "ws://" + Handshake.Host + Handshake.ResourcePath);

            if (isValid)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (handshake.AnswerBytes[i] != Handshake.ExpectedAnswer[i])
                    {
                        isValid = false;
                        break;
                    }
                        
                }
            }

            return isValid;
        }

        private void SendHandshake()
        {
            // generate a byte array representation of the handshake including the challenge
            byte[] hsBytes = Encoding.UTF8.GetBytes(Handshake.ToString());
            var challenge = Handshake.ChallengeBytes.Array;
            int hsBytesLength = hsBytes.Length;
            Array.Resize(ref hsBytes, hsBytesLength + challenge.Length);
            Array.Copy(challenge, 0, hsBytes, hsBytesLength, challenge.Length);

            Socket.Send(hsBytes);
        }

        private ServerHandshake ParseServerHandshake(ArraySegment<byte> byteShake)
        {
            // the "grammar" of the handshake
            var pattern = @"^HTTP\/1\.1 101 Web Socket Protocol Handshake\r\n" +  
                          @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+"; // unordered set of fields (name-chars colon space any-chars cr lf)

            // subtract the challenge bytes from the handshake
            var handshake = new ServerHandshake();
            ArraySegment<byte> challenge = new ArraySegment<byte>(byteShake.Array, byteShake.Count - 16, 16); 
            handshake.AnswerBytes = new byte[16];
            //challenge.Array;
            Array.Copy(challenge.Array, challenge.Offset, handshake.AnswerBytes, 0, 16);

            // get the rest of the handshake
            var utf8_handshake = Encoding.UTF8.GetString(byteShake.Array, 0, byteShake.Count - 16);

            // match the handshake against the "grammar"
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(utf8_handshake);
            var fields = match.Groups;


            // run through every match and save them in the handshake object
            for (int i = 0; i < fields["field_name"].Captures.Count; i++)
            {
                var name = fields["field_name"].Captures[i].ToString();
                var value = fields["field_value"].Captures[i].ToString();

                switch (name.ToLower())
                {
                    case "sec-websocket-origin":
                        handshake.Origin = value;
                        break;
                    case "sec-websocket-location":
                        handshake.Location = value;
                        break;
                    case "sec-websocket-protocol":
                        handshake.SubProtocol = value;
                        break;
                }
            }
            return handshake;
        }
            
    }
}
