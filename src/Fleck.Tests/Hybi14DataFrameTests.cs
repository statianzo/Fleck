using System;
using NUnit.Framework;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Fleck.Tests
{
    [TestFixture]
    public class Hybi14DataFrameTests
    {
        [Test]
        public void ShouldConvertToBytes()
        {
            var frame = new Hybi14DataFrame
            {
                IsFinal = true,
                IsMasked = false,
                Opcode = Opcode.Text,
                Payload = Encoding.UTF8.GetBytes("Hello")
            };
            
            var expected = new byte[]{ 129, 5, 72, 101, 108, 108, 111 };
            var actual = frame.ToBytes();
            
            Assert.AreEqual(expected, actual);
        }
        
        
        [Test]
        public void ShouldConvertPayloadsOver125BytesToBytes()
        {
            var frame = new Hybi14DataFrame
            {
                IsFinal = true,
                IsMasked = false,
                Opcode = Opcode.Text,
                Payload = Encoding.UTF8.GetBytes(new string('x', 140))
            };
            
            var expected = new List<byte>{ 129, 126, 0, 140};
            expected.AddRange(frame.Payload);
            
            var actual = frame.ToBytes();
            
            Assert.AreEqual(expected, actual.ToArray());
        }
    }
}

