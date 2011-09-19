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

        public Action<ISender, IReceiver> OnSuccess { get; set; }
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
            
            state.ByteCount += receivedByteCount;
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
            
            var buffer = state.Buffer;
            Array.Resize(ref buffer, state.ByteCount);
            var request = RequestParser.Parse(buffer);
            var builder = ResponseBuilderFactory.Resolve(request);
            if (builder == null)
            {
               FleckLog.Info("Incompatible request.");
               state.Socket.Close();
               return;
            }
            
            var response = builder.Build(request);

            FleckLog.Debug("Sending server handshake");
            state.Socket.Send(response, () => {
               EndSendServerHandshake(builder, state.Socket);
            },e => FleckLog.Error("Send handshake failed", e));
        }

       
        private void EndSendServerHandshake(IResponseBuilder builder, ISocket socket)
        {
            FleckLog.Debug("Ending server handshake");
            if (OnSuccess != null)
            {
                OnSuccess(builder.CreateSender(socket), builder.CreateReceiver(socket));
            }
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