using System;

namespace Fleck
{
    public static class WebSocketStatusCodes
    {
        public const ushort NormalClosure = 1000;
        public const ushort GoingAway = 1001;
        public const ushort ProtocolError = 1002;
        public const ushort UnsupportedDataType = 1003;
        public const ushort NoStatusReceived = 1005;
        public const ushort AbnormalClosure = 1006;
        public const ushort InvalidFramePayloadData = 1007;
        public const ushort PolicyViolation = 1008;
        public const ushort MessageTooBig = 1009;
        public const ushort MandatoryExt = 1010;
        public const ushort InternalServerError = 1011;
        public const ushort TLSHandshake = 1015;
        
        public const ushort ApplicationError = 3000;
        
        public static ushort[] ValidCloseCodes = new []{
            NormalClosure, GoingAway, ProtocolError, UnsupportedDataType,
            InvalidFramePayloadData, PolicyViolation, MessageTooBig,
            MandatoryExt, InternalServerError
        };

        public static string ConvertStatusCodeToString(ushort statusCode)
        {
            switch (statusCode)
            {
                case 1000:
                    return nameof(NormalClosure);
                case 1001:
                    return nameof(GoingAway);
                case 1002:
                    return nameof(ProtocolError);
                case 1003:
                    return nameof(UnsupportedDataType);
                case 1005:
                    return nameof(NoStatusReceived);
                case 1006:
                    return nameof(AbnormalClosure);
                case 1007:
                    return nameof(InvalidFramePayloadData);
                case 1008:
                    return nameof(PolicyViolation);
                case 1009:
                    return nameof(MessageTooBig);
                case 1010:
                    return nameof(MandatoryExt);
                case 1011:
                    return nameof(InternalServerError);
                case 1015:
                    return nameof(TLSHandshake);
                case 3000:
                    return nameof(ApplicationError);

                default:
                    throw new ArgumentException($"Unexpected status status code: {statusCode}", nameof(statusCode));
            }
        }
    }
}

