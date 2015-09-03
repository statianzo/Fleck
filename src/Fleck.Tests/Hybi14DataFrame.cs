using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;

namespace Fleck.Tests
{
    public struct Hybi14DataFrame
    {
        public bool IsFinal { get; set; }

        public FrameType FrameType { get; set; }

        public bool IsMasked { get; set; }

        public long PayloadLength { get { return Payload.Length; } }

        public int MaskKey { get; set; }

        public byte[] Payload { get; set; }
        
        public byte[] ToBytes()
        {
            var memoryStream = new MemoryStream();
            byte op = (byte)((byte)FrameType + (IsFinal ? 128 : 0));
            
            memoryStream.WriteByte(op);
            
             
            var payloadLengthBytes = GetLengthBytes();
             
            memoryStream.Write(payloadLengthBytes, 0, payloadLengthBytes.Length);
            
            var payload = Payload;
            if (IsMasked)
            {
                var keyBytes = BitConverter.GetBytes(MaskKey);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(keyBytes);
                memoryStream.Write(keyBytes, 0, keyBytes.Length);
                payload = TransformBytes(Payload, MaskKey);
            }
            
            memoryStream.Write(payload, 0, Payload.Length);
            
            return memoryStream.ToArray();
            
        }
        
        private byte[] GetLengthBytes()
        {
            var payloadLengthBytes = new List<byte>(9);
            
            if (PayloadLength > ushort.MaxValue)
            {
                payloadLengthBytes.Add(127);
                var lengthBytes = BitConverter.GetBytes(PayloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                payloadLengthBytes.AddRange(lengthBytes);
            }
            else if (PayloadLength > 125)
            {
                payloadLengthBytes.Add(126);
                var lengthBytes = BitConverter.GetBytes((UInt16)PayloadLength);
                if (BitConverter.IsLittleEndian)
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
        
        public static byte[] TransformBytes(byte[] bytes, int mask)
        {
            var output = new byte[bytes.Length];
            var maskBytes = BitConverter.GetBytes(mask);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(maskBytes);
            
            for (int i = 0; i < bytes.Length; i++)
            {
                output[i] = (byte)(bytes[i] ^ maskBytes[i % 4]);
            }
            
            return output;
        }
    }
}
