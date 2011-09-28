using System;

namespace Fleck
{
    public static class WebSocketStatusCodes
    {
        public const ushort ProtocolError = 1002;
        public const ushort UnsupportedDataType = 1003;
        public const ushort MessageTooBig = 1009;
        public const ushort ApplicationError = 3000;
    }
}

