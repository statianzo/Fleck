using System.Collections.Generic;

namespace Fleck
{
	public class ServerHandshake
	{
		public string Origin { get; set; }
		public string Location { get; set; }
		public byte[] AnswerBytes { get; set; }
		public string SubProtocol { get; set; }
		public Dictionary<string, string> AdditionalFields { get; set; }

		public string ToResponseString()
		{
			string stringShake = "HTTP/1.1 101 WebSocket Protocol Handshake\r\n" +
								 "Upgrade: WebSocket\r\n" +
								 "Connection: Upgrade\r\n" +
								 "Sec-WebSocket-Origin: " + Origin + "\r\n" +
								 "Sec-WebSocket-Location: " + Location + "\r\n";

			if (SubProtocol != null)
				stringShake += "Sec-WebSocket-Protocol: " + SubProtocol + "\r\n";
			stringShake += "\r\n";
			return stringShake;
		}
	}
}