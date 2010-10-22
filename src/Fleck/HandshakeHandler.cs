using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Fleck
{
	internal class HandshakeHandler
	{
		public HandshakeHandler(string origin, string location)
		{
			Origin = origin;
			Location = location;
		}

		public string Origin { get; set; }
		public string Location { get; set; }
		public ClientHandshake ClientHandshake { get; set; }
		public Action<ClientHandshake> OnSuccess { get; set; }


		public void Shake(Socket socket)
		{
			try
			{
				var state = new HandShakeState {socket = socket};
				state.socket.BeginReceive(state.buffer, 0, HandShakeState.BufferSize, 0, new AsyncCallback(DoShake), state);
			}
			catch (Exception e)
			{
				Log.Error("Exception thrown from method Receive:\n" + e.Message);
			}
		}

		private void DoShake(IAsyncResult ar)
		{
			var state = (HandShakeState) ar.AsyncState;
			int receivedByteCount = state.socket.EndReceive(ar);

			ClientHandshake = ParseClientHandshake(new ArraySegment<byte>(state.buffer, 0, receivedByteCount));

			bool hasRequiredFields = (ClientHandshake.ChallengeBytes != null) &&
			                         (ClientHandshake.Host != null) &&
			                         (ClientHandshake.Key1 != null) &&
			                         (ClientHandshake.Key2 != null) &&
			                         (ClientHandshake.Origin != null) &&
			                         (ClientHandshake.ResourcePath != null);

			if (hasRequiredFields && "ws://" + ClientHandshake.Host == Location && ClientHandshake.Origin == Origin)
			{
				ServerHandshake serverShake = GenerateResponseHandshake();
				BeginSendServerHandshake(serverShake, state.socket);
			}
			else
			{
				state.socket.Close();
				return;
			}
		}

		private ClientHandshake ParseClientHandshake(ArraySegment<byte> byteShake)
		{
			const string pattern = @"^(?<connect>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
			                       @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+";

			var handshake = new ClientHandshake();
			var challenge = new ArraySegment<byte>(byteShake.Array, byteShake.Count - 8, 8); // -8 : eight byte challenge
			handshake.ChallengeBytes = challenge;

			string utf8_handshake = Encoding.UTF8.GetString(byteShake.Array, 0, byteShake.Count - 8);

			var regex = new Regex(pattern, RegexOptions.IgnoreCase);
			Match match = regex.Match(utf8_handshake);
			GroupCollection fields = match.Groups;

			handshake.ResourcePath = fields["path"].Value;

			for (int i = 0; i < fields["field_name"].Captures.Count; i++)
			{
				string name = fields["field_name"].Captures[i].ToString();
				string value = fields["field_value"].Captures[i].ToString();

				switch (name.ToLower())
				{
					case "sec-websocket-key1":
						handshake.Key1 = value;
						break;
					case "sec-websocket-key2":
						handshake.Key2 = value;
						break;
					case "sec-websocket-protocol":
						handshake.SubProtocol = value;
						break;
					case "origin":
						handshake.Origin = value;
						break;
					case "host":
						handshake.Host = value;
						break;
					case "cookie":
						handshake.Cookies = value;
						break;
					default:
						if (handshake.AdditionalFields == null)
							handshake.AdditionalFields = new Dictionary<string, string>();
						handshake.AdditionalFields[name] = value;
						break;
				}
			}
			return handshake;
		}

		private ServerHandshake GenerateResponseHandshake()
		{
			var responseHandshake = new ServerHandshake
			{
				Location = "ws://" + ClientHandshake.Host + ClientHandshake.ResourcePath,
				Origin = ClientHandshake.Origin,
				SubProtocol = ClientHandshake.SubProtocol
			};

			var challenge = new byte[8];
			Array.Copy(ClientHandshake.ChallengeBytes.Array, ClientHandshake.ChallengeBytes.Offset, challenge, 0, 8);

			responseHandshake.AnswerBytes =
				CalculateAnswerBytes(ClientHandshake.Key1, ClientHandshake.Key2, ClientHandshake.ChallengeBytes);

			return responseHandshake;
		}

		private void BeginSendServerHandshake(ServerHandshake handshake, Socket socket)
		{
			string stringShake = "HTTP/1.1 101 Web Socket Protocol Handshake\r\n" +
			                     "Upgrade: WebSocket\r\n" +
			                     "Connection: Upgrade\r\n" +
			                     "Sec-WebSocket-Origin: " + handshake.Origin + "\r\n" +
			                     "Sec-WebSocket-Location: " + handshake.Location + "\r\n";

			if (handshake.SubProtocol != null)
			{
				stringShake += "Sec-WebSocket-Protocol: " + handshake.SubProtocol + "\r\n";
			}
			stringShake += "\r\n";


			byte[] byteResponse = Encoding.ASCII.GetBytes(stringShake);
			int byteResponseLength = byteResponse.Length;
			Array.Resize(ref byteResponse, byteResponseLength + handshake.AnswerBytes.Length);
			Array.Copy(handshake.AnswerBytes, 0, byteResponse, byteResponseLength, handshake.AnswerBytes.Length);

			socket.BeginSend(byteResponse, 0, byteResponse.Length, 0, EndSendServerHandshake, socket);
		}

		private void EndSendServerHandshake(IAsyncResult ar)
		{
			var socket = (Socket) ar.AsyncState;
			socket.EndSend(ar);

			if (OnSuccess != null)
			{
				OnSuccess(ClientHandshake);
			}
		}

		private static byte[] CalculateAnswerBytes(string key1, string key2, ArraySegment<byte> challenge)
		{
			byte[] result1Bytes = ParseKey(key1);
			byte[] result2Bytes = ParseKey(key2);

			var rawAnswer = new byte[16];
			Array.Copy(result1Bytes, 0, rawAnswer, 0, 4);
			Array.Copy(result2Bytes, 0, rawAnswer, 4, 4);
			Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);

			MD5 md5 = MD5.Create();
			return md5.ComputeHash(rawAnswer);
		}

		private static byte[] ParseKey(string key1)
		{
			int spaces = key1.Count(x => x == ' ');
			var digits = new String(key1.Where(Char.IsDigit).ToArray());

			var value = (Int32) (Int64.Parse(digits)/spaces);

			byte[] result = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(result);
			return result;
		}

		#region Nested type: HandShakeState

		private class HandShakeState
		{
			public const int BufferSize = 1024;
			public readonly byte[] buffer = new byte[BufferSize];
			public Socket socket;
		}

		#endregion
	}
}