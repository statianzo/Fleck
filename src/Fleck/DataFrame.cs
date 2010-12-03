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

		private readonly StringBuilder _builder;
		private readonly List<byte> _buffer;

		public DataFrame()
		{
			_builder = new StringBuilder();
			_buffer = new List<byte>();
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

		public void Append(byte data)
		{
			if(data == Start)
				return;
			if(data == End)
			{
				IsComplete = true;
    			_builder.Append(Encoding.UTF8.GetString(_buffer.ToArray()));
				return;
			}
			_buffer.Add(data);

		}

		public override string ToString()
		{
			return _builder != null ? _builder.ToString() : "";
		}
	}
}