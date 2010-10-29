using System;
using System.Collections.Generic;

namespace Fleck
{
	public class ClientHandshake
	{
		public string Origin { get; set; }
		public string Host { get; set; }
		public string ResourcePath { get; set; }
		public string Key1 { get; set; }
		public string Key2 { get; set; }
		public ArraySegment<byte> ChallengeBytes { get; set; }
		public string Cookies { get; set; }
		public string SubProtocol { get; set; }
		public Dictionary<string, string> AdditionalFields { get; set; }

		public override string ToString()
		{
			string stringShake = "GET " + ResourcePath + " HTTP/1.1\r\n" +
			                     "Upgrade: WebSocket\r\n" +
			                     "Connection: Upgrade\r\n" +
			                     "Origin: " + Origin + "\r\n" +
			                     "Host: " + Host + "\r\n" +
			                     "Sec-Websocket-Key1: " + Key1 + "\r\n" +
			                     "Sec-Websocket-Key2: " + Key2 + "\r\n";


			if (Cookies != null)
			{
				stringShake += "Cookie: " + Cookies + "\r\n";
			}
			if (SubProtocol != null)
				stringShake += "Sec-Websocket-Protocol: " + SubProtocol + "\r\n";

			if (AdditionalFields != null)
			{
				foreach (KeyValuePair<string, string> field in AdditionalFields)
				{
					stringShake += field.Key + ": " + field.Value + "\r\n";
				}
			}
			stringShake += "\r\n";

			return stringShake;
		}

		public bool Validate(string origin, string host)
		{
			bool hasRequiredFields = (Host != null) &&
			                         (Key1 != null) &&
			                         (Key2 != null) &&
			                         (Origin != null) &&
			                         (ResourcePath != null);

			return hasRequiredFields && "ws://" + Host == host && (origin == null || origin == Origin);

		}
	}
}