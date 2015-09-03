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
        public static IHandler Create(WebSocketHttpRequest request, Action<string> onMessage, Action onClose, Action<byte[]> onBinary, Action<byte[]> onPing, Action<byte[]> onPong)
        {
            var readState = new ReadState();
            return new ComposableHandler
            {
                Handshake = sub => Hybi13Handler.BuildHandshake(request, sub),
                TextFrame = s => Hybi13Handler.FrameData(Encoding.UTF8.GetBytes(s), FrameType.Text),
                BinaryFrame = s => Hybi13Handler.FrameData(s, FrameType.Binary),
                PingFrame = s => Hybi13Handler.FrameData(s, FrameType.Ping),
                PongFrame = s => Hybi13Handler.FrameData(s, FrameType.Pong),
                CloseFrame = i => Hybi13Handler.FrameData(i.ToBigEndianBytes<ushort>(), FrameType.Close),
                ReceiveData = d => Hybi13Handler.ReceiveData(d, readState, (op, data) => Hybi13Handler.ProcessFrame(op, data, onMessage, onClose, onBinary, onPing, onPong))
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
                var reservedBits = (data[0] & 112);
                var frameType = (FrameType)(data[0] & 15);
                var isMasked = (data[1] & 128) != 0;
                var length = (data[1] & 127);
                
                
                if (!isMasked
                    || !Enum.IsDefined(typeof(FrameType), frameType)
                    || reservedBits != 0 //Must be zero per spec 5.2
                    || (frameType == FrameType.Continuation && !readState.FrameType.HasValue))
                    throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                
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
        
        public static void ProcessFrame(FrameType frameType, byte[] data, Action<string> onMessage, Action onClose, Action<byte[]> onBinary, Action<byte[]> onPing, Action<byte[]> onPong)
        {
            switch (frameType)
            {
            case FrameType.Close:
                if (data.Length == 1 || data.Length>125)
                    throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                    
                if (data.Length >= 2)
                {
                    var closeCode = (ushort)data.Take(2).ToArray().ToLittleEndianInt();
                    if (!WebSocketStatusCodes.ValidCloseCodes.Contains(closeCode) && (closeCode < 3000 || closeCode > 4999))
                        throw new WebSocketException(WebSocketStatusCodes.ProtocolError);
                }
                
                if (data.Length > 2)
                    ReadUTF8PayloadData(data.Skip(2).ToArray());
                
                onClose();
                break;
            case FrameType.Binary:
                onBinary(data);
                break;
            case FrameType.Ping:
                onPing(data);
                break;
            case FrameType.Pong:
                onPong(data);
                break;
            case FrameType.Text:
                onMessage(ReadUTF8PayloadData(data));
                break;
            default:
                FleckLog.Debug("Received unhandled " + frameType);
                break;
            }
        }
        
        
        public static byte[] BuildHandshake(WebSocketHttpRequest request, string subProtocol)
        {
            FleckLog.Debug("Building Hybi-14 Response");
            
            var builder = new StringBuilder();

            builder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");
            if (subProtocol != null)
              builder.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", subProtocol);

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
        
        private static string ReadUTF8PayloadData(byte[] bytes)
        {
            var encoding = new UTF8Encoding(false, true);
            try
            {
                return encoding.GetString(bytes);
            }
            catch(ArgumentException)
            {
                throw new WebSocketException(WebSocketStatusCodes.InvalidFramePayloadData);
            }
        }
    }

}
