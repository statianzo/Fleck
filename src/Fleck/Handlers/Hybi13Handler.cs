using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Fleck.Handlers
{
    public static class Hybi13Handler
    {
        public static IHandler Create(WebSocketHttpRequest request, Action<string> onMessage, Action onClose)
        {
            var readState = new ReadState();
            return new ComposableHandler
            {
                Handshake = () => Hybi13Handler.BuildHandshake(request),
                Frame = s => Hybi13Handler.FrameData(Encoding.UTF8.GetBytes(s), FrameType.Text),
                Close = i => Hybi13Handler.FrameData(i.ToBigEndianBytes<ushort>(), FrameType.Close),
                ReceiveData = d => Hybi13Handler.ReceiveData(d, readState, (op, data) => Hybi13Handler.ProcessFrame(op, data, onMessage, onClose))
            };
        }
        
        public static byte[] FrameData(byte[] payload, FrameType frameType)
        {
            var memoryStream = new MemoryStream();
            byte op = (byte)((byte)frameType + 128);
            
            memoryStream.WriteByte(op);
            
            if (payload.Length > UInt16.MaxValue) {
                memoryStream.WriteByte(127);
                var lengthBytes = payload.Length.ToBigEndianBytes<ulong>();
                memoryStream.Write(lengthBytes, 0, lengthBytes.Length);
            } else if (payload.Length > 125) {
                memoryStream.WriteByte(126);
                var lengthBytes = payload.Length.ToBigEndianBytes<ushort>();
                memoryStream.Write(lengthBytes, 0, lengthBytes.Length);
            } else {
                memoryStream.WriteByte((byte)payload.Length);
            }
            
            memoryStream.Write(payload, 0, payload.Length);
            
            return memoryStream.ToArray();
        }
        
        public static void ReceiveData(List<byte> data, ReadState readState, Action<FrameType, byte[]> processFrame)
        {
            
            while (data.Count >= 2)
            {
                var isFinal = (data[0] & 128) != 0;
                var frameType = (FrameType)(data[0] & 15);
                var isMasked = (data[1] & 128) != 0;
                var length = (data[1] & 127);
                
                if (!isMasked)
                    throw new WebSocketException("Client data must be masked");

                if (frameType == FrameType.Continuation && !readState.FrameType.HasValue)
                    throw new WebSocketException("Unexpected continuation frame received");
                
                var index = 2;
                int payloadLength;
                
                if (length == 127)
                {
                    if (data.Count < index + 8)
                        return; //Not complete
                    payloadLength = data.Skip(index).Take(8).ToArray().ToLittleEndianInt();
                    index += 8;
                }
                else if (length == 126)
                {
                    if (data.Count < index + 2)
                        return; //Not complete
                    payloadLength = data.Skip(index).Take(2).ToArray().ToLittleEndianInt();
                    index += 2;
                }
                else
                {
                    payloadLength = length;
                }
                
                if (data.Count < index + 4) 
                    return; //Not complete
               
                var maskBytes = data.Skip(index).Take(4).ToArray();
                
                index += 4;
                
                
                if (data.Count < index + payloadLength) 
                    return; //Not complete
                
                var payload = data
                                .Skip(index)
                                .Take(payloadLength)
                                .Select((x, i) => (byte)(x ^ maskBytes[i % 4]));
                 
                 
                readState.Data.AddRange(payload);
                data.RemoveRange(0, index + payloadLength);
                
                if (frameType != FrameType.Continuation)
                    readState.FrameType = frameType;
                
                if (isFinal && readState.FrameType.HasValue)
                {
                    var stateData = readState.Data.ToArray();
                    var stateFrameType = readState.FrameType;
                    readState.Clear();
                    
                    processFrame(stateFrameType.Value, stateData);
                }
            }
        }
        
        public static void ProcessFrame(FrameType frameType, byte[] data, Action<string> onMessage, Action onClose)
        {
            switch (frameType)
            {
            case FrameType.Close:
                onClose();
                break;
            case FrameType.Binary:
            case FrameType.Text:
                onMessage(Encoding.UTF8.GetString(data));
                break;
            default:
                FleckLog.Debug("Received unhandled " + frameType);
                break;
            }
        }
        
        
        public static byte[] BuildHandshake(WebSocketHttpRequest request)
        {
            FleckLog.Debug("Building Hybi-14 Response");
            
            var builder = new StringBuilder();

            builder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");

            var responseKey =  CreateResponseKey(request["Sec-WebSocket-Key"]);
            builder.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", responseKey);
            builder.Append("\r\n");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        
        public static string CreateResponseKey(string requestKey)
        {
            var combined = requestKey + WebSocketResponseGuid;

            var bytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(combined));

            return Convert.ToBase64String(bytes);
        }
    }

}
