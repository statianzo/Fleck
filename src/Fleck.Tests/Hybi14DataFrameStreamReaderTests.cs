using System;
using NUnit.Framework;
using System.Text;
using System.IO;
using System.Threading;

namespace Fleck.Tests
{
    [TestFixture]
    public class Hybi14DataFrameStreamReaderTests
    {
        [Test]
        public void ShouldReadDataFrame()
        {
            var text = "This is a message";
            
            var dataFrame = new Hybi14DataFrame
            {
                IsFinal = true,
                MaskKey = 42353245,
                IsMasked = true,
                Opcode = Opcode.Text,
                Payload = Encoding.UTF8.GetBytes(text)
            };
            
            var bytes = dataFrame.ToBytes();
            var stream = new MemoryStream(bytes);
            
            var reader = new Hybi14DataFrameStreamReader(stream);
            
            var ev = new EventWaitHandle(false, EventResetMode.ManualReset);
            
            Hybi14DataFrame actual = default(Hybi14DataFrame);
            reader.ReadFrame(x => {
                actual = x;
                ev.Set();
            }, e => {throw e;});
            
            ev.WaitOne();
            
            Assert.AreEqual(dataFrame.IsFinal, actual.IsFinal);
            Assert.AreEqual(dataFrame.IsMasked, actual.IsMasked);
            Assert.AreEqual(dataFrame.PayloadLength, actual.PayloadLength);
            Assert.AreEqual(dataFrame.Opcode, actual.Opcode);
            Assert.AreEqual(dataFrame.MaskKey, actual.MaskKey);
            Assert.AreEqual(text, Encoding.UTF8.GetString(actual.Payload));
            Assert.AreEqual(dataFrame.Payload, actual.Payload);
            
            
        }
    
    }
}

