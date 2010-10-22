using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fleck
{
	public class DataFrame
	{
		public const byte End = 255;
		public const byte Start = 0;

		private readonly StringBuilder builder;

		public DataFrame()
		{
			IsComplete = false;
			builder = new StringBuilder();
		}

		public bool IsComplete { get; set; }

		public static byte[] Wrap(string data)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			// wrap the array with the wrapper bytes
			var wrappedBytes = new byte[bytes.Length + 2];
			wrappedBytes[0] = Start;
			wrappedBytes[wrappedBytes.Length - 1] = End;
			Array.Copy(bytes, 0, wrappedBytes, 1, bytes.Length);
			return wrappedBytes;
		}

		public void Append(byte[] data)
		{
			int start = 0, end = data.Length - 1;

			List<byte> bufferList = data.ToList();

			bool endIsInThisBuffer = data.Contains(End);
			if (endIsInThisBuffer)
			{
				end = bufferList.IndexOf(End);
				end--; // we dont want to include this byte
			}

			bool startIsInThisBuffer = data.Contains(Start); // 0 = start
			if (startIsInThisBuffer)
			{
				int zeroPos = bufferList.IndexOf(Start);
				if (zeroPos < end) // we might be looking at one of the bytes in the end of the array that hasn't been set
				{
					start = zeroPos;
					start++; // we dont want to include this byte
				}
			}

			builder.Append(Encoding.UTF8.GetString(data, start, (end - start) + 1));

			IsComplete = endIsInThisBuffer;
		}

		public override string ToString()
		{
			return builder != null ? builder.ToString() : "";
		}
	}
}