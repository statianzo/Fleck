using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
namespace Fleck
{
    public struct Hybi14DataFrame
    {
        public bool IsFinal { get; set; }

        public Opcode Opcode { get; set; }

        public bool IsMasked { get; set; }

        public long PayloadLength { get { return Payload.Length; } }

        public int MaskKey { get; set; }

        public byte[] Payload { get; set; }
        
        public byte[] ToBytes()
        {
            var memoryStream = new MemoryStream();
            byte op = (byte)((byte)Opcode + (IsFinal ? 128 : 0));
            
            memoryStream.WriteByte(op);
            
             
             var payloadLengthBytes = GetLengthBytes();
             
            memoryStream.Write(payloadLengthBytes, 0, payloadLengthBytes.Length);
            
            memoryStream.Write(Payload, 0, Payload.Length);
            
            return memoryStream.ToArray();
            
        }
        
        private byte[] GetLengthBytes()
        {
            var payloadLengthBytes = new List<byte>(9);
            
            if (PayloadLength > ushort.MaxValue)
            {
                payloadLengthBytes.Add(127);
                var lengthBytes = BitConverter.GetBytes(PayloadLength);
                Array.Reverse(lengthBytes);
                payloadLengthBytes.AddRange(lengthBytes);
            }
            else if (PayloadLength > 125)
            {
                payloadLengthBytes.Add(126);
                var lengthBytes = BitConverter.GetBytes((UInt16)PayloadLength);
                Array.Reverse(lengthBytes);
                payloadLengthBytes.AddRange(lengthBytes);
            }
            else
            {
                payloadLengthBytes.Add((byte)PayloadLength);
            }
            
            payloadLengthBytes[0] += (byte)(IsMasked ? 128 : 0);
            
            return payloadLengthBytes.ToArray();
        }
    }

    public enum Opcode : byte
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }
}