using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fleck
{
	public class Sender
	{
		private readonly WebSocketConnection _connection;

		public Sender(WebSocketConnection connection)
		{
			_connection = connection;
		}

		public Socket Socket { get { return _connection.Socket; } }

		public void Send(string data)
		{
			if (!Socket.Connected) return;
			var wrapped = DataFrame.Wrap(data);
			var segment = new ArraySegment<byte>(wrapped);

			Task<int>.Factory.FromAsync(Socket.BeginSend, Socket.EndSend, new[] {segment}, SocketFlags.None, null)
				.ContinueWith(t =>
					{
						if (t.Exception == null) return;
						FleckLog.Error(t.Exception.Message);
						_connection.Close();
					}, TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}