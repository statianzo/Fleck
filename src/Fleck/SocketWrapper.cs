using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Fleck
{
	public class SocketWrapper : ISocket
	{
		private readonly Socket _socket;

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
		}

		public EndPoint LocalEndPoint
		{
			get { return _socket.LocalEndPoint; }
		}

		public void Listen(int backlog)
		{
			_socket.Listen(backlog);
		}

		public void Bind(EndPoint endPoint)
		{
			_socket.Bind(endPoint);
		}

		public bool Connected
		{
			get { return _socket.Connected; }
		}

		public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback,
		                                 object state)
		{
			return _socket.BeginReceive(buffers, socketFlags, callback, state);
		}

		public int EndReceive(IAsyncResult asyncResult)
		{
			return _socket.EndReceive(asyncResult);
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return _socket.BeginAccept(callback, state);
		}

		public ISocket EndAccept(IAsyncResult asyncResult)
		{
			return new SocketWrapper(_socket.EndAccept(asyncResult));
		}

		public void Dispose()
		{
			_socket.Dispose();
		}

		public void Close()
		{
			_socket.Close();
		}

		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback,
		                              object state)
		{
			return _socket.BeginSend(buffers, socketFlags, callback, state);
		}

		public int EndSend(IAsyncResult asyncResult)
		{
			return _socket.EndSend(asyncResult);
		}
	}
}