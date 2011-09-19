using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Fleck
{
    public class Hybi14DataFrameStreamReader
    {
        Stream _stream;
        
        public Hybi14DataFrameStreamReader(Stream stream)
        {
            _stream = stream;
        }
        
        public void ReadFrame(Action<Hybi14DataFrame> success, Action<Exception> error)
        {
            
            byte[] buffer = new byte[16384];
            
            var task = CreateReadTask(2, buffer);
            
            var frame = new Hybi14DataFrame();
            var readPayloadTask = task.ContinueWith(t => {
                var nextRead = 0;
                frame.IsFinal = (buffer[0] & 128) != 0;
                frame.Opcode = (Opcode)(buffer[0] & 15);
                frame.IsMasked = (buffer[1] & 128) != 0;
                var payloadLength = (buffer[1] & 127);
                
                switch(payloadLength)
                {
                case 127:
                    nextRead += 8;
                    break;
                case 126:
                    nextRead += 2;
                    break;
                default:
                    nextRead = payloadLength;
                    if(frame.IsMasked)
                        nextRead += 4;
                    break;
                }
                
                var nextTask = CreateReadTask(nextRead, buffer);
                if (payloadLength > 125)
                    nextTask = nextTask.ContinueWith(n => {
                        int actualPayloadSize;
                        if (payloadLength == 126)
                            actualPayloadSize = BitConverter.ToInt16(buffer,0);
                        else
                            actualPayloadSize = (int)BitConverter.ToInt64(buffer, 0);
                        
                        if(frame.IsMasked)
                            actualPayloadSize += 4;
                        
                        return CreateReadTask(actualPayloadSize, buffer);
                    }).Unwrap();
                
                
                
                return nextTask;
            }, TaskContinuationOptions.NotOnFaulted).Unwrap();
            
            readPayloadTask.ContinueWith(t => {
                byte[] payload;
                
                if (frame.IsMasked)
                {
                    var maskBytes = buffer.Take(4).ToArray();
                    frame.MaskKey = BitConverter.ToInt32(maskBytes, 0);
                    payload = Hybi14DataFrame.TransformBytes(buffer.Skip(4).Take(t.Result - 4).ToArray(), frame.MaskKey);
                }
                else
                {
                    payload = buffer.Take(t.Result).ToArray();
                }
                
                frame.Payload = payload;
                
                success(frame);
            });
            
            Action<Task<int>> onError = t => error(t.Exception);
            
            task.ContinueWith(onError, TaskContinuationOptions.OnlyOnFaulted);
            readPayloadTask.ContinueWith(onError, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        private Task<int> CreateReadTask(int length, byte[] buffer)
        {
            return Task.Factory.FromAsync<int,int>(BeginRead, _stream.EndRead, length, buffer);
        }
        
        private IAsyncResult BeginRead(int length, AsyncCallback cb, object state)
        {
            return _stream.BeginRead((byte[])state, 0, length, cb, state);
        }
        
    }
}

