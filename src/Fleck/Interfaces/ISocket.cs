using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Fleck
{
	public interface ISocket
	{
		bool Connected { get; }

		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback,
		                                          object state);

		int EndReceive(IAsyncResult asyncResult);
		IAsyncResult BeginAccept(AsyncCallback callback, object state);
		ISocket EndAccept(IAsyncResult asyncResult);
		void Dispose();
		void Close();

		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback,
		                                       object state);

		int EndSend(IAsyncResult asyncResult);
		void Bind(EndPoint ipLocal);
		void Listen(int backlog);
	}
}