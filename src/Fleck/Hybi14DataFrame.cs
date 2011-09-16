namespace Fleck
{
    public class Hybi14DataFrame
    {
        public bool IsFinal { get; set; }

        public Opcode Opcode { get; set; }

        public bool IsMasked { get; set; }

        public uint PayloadLength { get; set; }

        public int MaskKey { get; set; }

        public byte[] Payload { get; set; }
    }

    public enum Opcode
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }
}