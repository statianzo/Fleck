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
	}
}