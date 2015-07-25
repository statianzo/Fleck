using System;
using System.Collections.Generic;

namespace Fleck
{
    public class ReadState
    {
        public ReadState()
        {
            Data = new List<byte>();
            FragmentNumber = 1;
        }
        public List<byte> Data { get; private set; }
        public FrameType? FrameType { get; set; }
        public int FragmentNumber { get; set; }
        public void Clear()
        {
            Data.Clear();
            FrameType = null;
            FragmentNumber = 1;
        }
    }
}

