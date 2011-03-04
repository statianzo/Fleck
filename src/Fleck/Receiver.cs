using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fleck
{
	public class Receiver
	{
		private const int BufferSize = 16384;
		private readonly WebSocketConnection _connection;
		private readonly Queue<byte> _queue;

		public Receiver(WebSocketConnection connection)
		{
			_connection = connection;
			_queue = new Queue<byte>();
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
				_connection.Close();
				return;
			}
			var segment = new ArraySegment<byte>(buffer);

			Task<int>.Factory.FromAsync(Socket.BeginReceive, Socket.EndReceive, new[] { segment }, SocketFlags.None, null)
				.ContinueWith(t =>
					{
						int size = t.Result;
						var dataframe = frame;

						if (size <= 0)
						{
							_connection.Close();
							return;
						}

						for (int i = 0; i < size; i++)
							_queue.Enqueue(buffer[i]);

						while (_queue.Count > 0)
						{
							dataframe.Append(_queue.Dequeue());
							if (!dataframe.IsComplete) continue;

							var data = dataframe.ToString();
							_connection.OnMessage(data);
							dataframe = new DataFrame();
						}
						Receive(dataframe);
					}, TaskContinuationOptions.NotOnFaulted)
				.ContinueWith(t =>
					{
						if (t.Exception == null) return;
						FleckLog.Error(t.Exception.Message);
						_connection.Close();
					}, TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}