using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fleck
{
	public class Sender
	{
		private readonly ISocket _socket;
		private readonly Action _closeAction;

		public Sender(ISocket socket, Action closeAction)
		{
			_socket = socket;
			_closeAction = closeAction;
		}

		public ISocket Socket { get { return _socket; } }

		public void Send(string data)
		{
			if (!Socket.Connected) return;
			var wrapped = DataFrame.Wrap(data);
			var segment = new ArraySegment<byte>(wrapped);
			Func<AsyncCallback, object,IAsyncResult> begin = (cb, s) => Socket.BeginSend(new []{segment},SocketFlags.None,cb, s);

			Task.Factory.FromAsync<int>(begin, Socket.EndSend, null)
				.ContinueWith(t =>
					{
						FleckLog.Error("Send failed", t.Exception);
						_closeAction();
					}, TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}