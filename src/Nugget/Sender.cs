using System.Net.Sockets;

namespace Nugget
{
	internal class Sender
	{
		private readonly WebSocketConnection _connection;

		public Sender(WebSocketConnection connection)
		{
			_connection = connection;
		}

		public Socket Socket { get { return _connection.Socket; } }

		public void Send(string data)
		{
			if (Socket.Connected)
			{
				Socket.AsyncSend(DataFrame.Wrap(data),
								 byteCount => Log.Debug(byteCount + " bytes send to " + Socket.RemoteEndPoint));
			}
			else
			{
				_connection.OnClose();
				Socket.Close();
			}
		}
	}
}