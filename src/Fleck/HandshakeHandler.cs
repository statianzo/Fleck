using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Fleck.Interfaces;

namespace Fleck
{
    public class HandshakeHandler
    {
        public HandshakeHandler(IResponseBuilderFactory factory)
        {
            RequestParser = new RequestParser();
            ResponseBuilderFactory = factory;
        }

        public ClientHandshake ClientHandshake { get; set; }
        public Action<ClientHandshake> OnSuccess { get; set; }
        public IRequestParser RequestParser { get; set; }
        public IResponseBuilderFactory ResponseBuilderFactory { get; set; }

        public void Shake(ISocket socket, HandShakeState state = null)
        {
            state = state ?? new HandShakeState { Socket = socket };

            socket.Receive(state.Buffer,
                           r => DoShake(state, r),
                           e => FleckLog.Error("Failed to recieve handshake", e),
                           state.ByteCount);
        }

        public void DoShake(HandShakeState state, int receivedByteCount)
        {
            FleckLog.Debug("Recieving Request");
            
            if (receivedByteCount == 0)
            {
                FleckLog.Info("No bytes recieved. Connection closed.");
                state.Socket.Close();
                return;
            }
            
            if (!RequestParser.IsComplete(state.Buffer))
            {
               FleckLog.Debug("Request Incomplete. Requesting more.");
               Shake(state.Socket, state);
               return;
            }
            
            var request = RequestParser.Parse(state.Buffer);
            var builder = ResponseBuilderFactory.Resolve(request);
            if (builder == null)
            {
               FleckLog.Info("Incompatible request.");
               state.Socket.Close();
               return;
            }
            
            var response = builder.Build(request);

            FleckLog.Debug("Sending server handshake");
            state.Socket.Send(response,
                        EndSendServerHandshake,
                        e => FleckLog.Error("Send handshake failed", e));
        }

       
        private void EndSendServerHandshake()
        {
            FleckLog.Debug("Ending server handshake");
            if (OnSuccess != null)
                OnSuccess(ClientHandshake);
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

        public class HandShakeState
        {
            private const int BufferSize = 1024;
            public readonly byte[] Buffer = new byte[BufferSize];
            public ISocket Socket { get; set; }
            public int ByteCount { get; set; }
        }
    }
}