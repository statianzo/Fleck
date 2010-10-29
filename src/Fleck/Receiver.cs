using System.Net.Sockets;

namespace Fleck
{
	internal class Receiver
	{
		public const int BufferSize = 256;
		private readonly WebSocketConnection _connection;

		public Receiver(WebSocketConnection connection)
		{
			_connection = connection;
		}

		private Socket Socket
		{
			get { return _connection.Socket; }
		}

		public void Receive(DataFrame frame = null)
		{
			if (frame == null)
				frame = new DataFrame();

			var buffer = new byte[BufferSize];

			if (Socket == null || !Socket.Connected)
			{
				_connection.OnClose();
				return;
			}

			Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None,
				r =>
				{
					int sizeOfReceivedData = Socket.EndReceive(r);
					var dataframe = frame;

					if (sizeOfReceivedData > 0)
					{
						dataframe.Append(buffer);

						if (dataframe.IsComplete)
						{
							string data = dataframe.ToString();

							_connection.OnMessage(data);


							Receive();
						}
						else
						{
							Receive(dataframe);
						}
					}
					else
					{
						_connection.OnClose();
						Socket.Close();
					}
				}, null);
		}
	}
}