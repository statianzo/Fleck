using System;
using System.Net.Sockets;

namespace Fleck
{
	public static class SocketExtension
	{
		// class that wraps the two different kinds of callbacks (with or without state object)

		#region Nested type: Callback

		private class Callback
		{
			private readonly Action<int> cb;
			private readonly Action<int, object> cbWithState;

			public Callback(Action<int> callback)
			{
				cb = callback;
			}

			public Callback(Action<int, object> callback)
			{
				cbWithState = callback;
			}

			public IAsyncResult BeginInvoke(int arg, AsyncCallback callback, object obj)
			{
				if (cb != null)
				{
					return cb.BeginInvoke(arg, callback, obj);
				}
				else
				{
					throw new InvalidCastException("callback<int> is not set");
				}
			}

			public IAsyncResult BeginInvoke(int arg, object state, AsyncCallback callback, object obj)
			{
				if (cbWithState != null)
				{
					return cbWithState.BeginInvoke(arg, state, callback, obj);
				}
				else
				{
					throw new InvalidCastException("callback<int,object> is not set");
				}
			}

			public void EndInvoke(IAsyncResult ar)
			{
				if (cb != null)
				{
					cb.EndInvoke(ar);
				}
				else
				{
					cbWithState.EndInvoke(ar);
				}
			}
		}

		#endregion

		#region Nested type: State

		private class State
		{
			public Socket Socket { get; set; }
			public Callback Callback { get; set; }
			public object UserDefinedState { get; set; }
		}

		#endregion

		#region Send

		public static void AsyncSend(this Socket socket, byte[] buffer, object state, Action<int, object> callback)
		{
			socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback),
			                 new State {Socket = socket, Callback = new Callback(callback), UserDefinedState = state});
		}

		public static void AsyncSend(this Socket socket, byte[] buffer, Action<int> callback)
		{
			socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback),
			                 new State {Socket = socket, Callback = new Callback(callback)});
		}

		public static void AsyncSend(this Socket socket, byte[] buffer)
		{
			socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback),
			                 new State {Socket = socket, Callback = null});
		}

		private static void SendCallback(IAsyncResult ar)
		{
			var state = (State) ar.AsyncState;
			int count = state.Socket.EndSend(ar);
			if (state.Callback != null)
			{
				if (state.UserDefinedState != null)
				{
					state.Callback.BeginInvoke(count, state.UserDefinedState, SendCallbackCallback, state);
				}
				else
				{
					state.Callback.BeginInvoke(count, new AsyncCallback(SendCallbackCallback), state);
				}
			}
		}

		private static void SendCallbackCallback(IAsyncResult ar)
		{
			var state = (State) ar.AsyncState;
			state.Callback.EndInvoke(ar);
		}

		#endregion

		#region Receive

		public static void AsyncReceive(this Socket socket, byte[] buffer, object state, Action<int, object> callback)
		{
			if (state == null)
				throw new InvalidOperationException("State cannot be null");

			socket.BeginReceive(buffer, 0, buffer.Length, 0, new AsyncCallback(ReceiveCallback),
			                    new State {Socket = socket, Callback = new Callback(callback), UserDefinedState = state});
		}

		public static void AsyncReceive(this Socket socket, byte[] buffer, Action<int> callback)
		{
			socket.BeginReceive(buffer, 0, buffer.Length, 0, new AsyncCallback(ReceiveCallback),
			                    new State {Socket = socket, Callback = new Callback(callback)});
		}

		public static void AsyncReceive(this Socket socket, byte[] buffer)
		{
			socket.BeginReceive(buffer, 0, buffer.Length, 0, new AsyncCallback(ReceiveCallback),
			                    new State {Socket = socket, Callback = null});
		}

		private static void ReceiveCallback(IAsyncResult ar)
		{
			var state = (State) ar.AsyncState;
			int count = state.Socket.EndReceive(ar);
			if (state.Callback != null)
			{
				if (state.UserDefinedState != null)
				{
					state.Callback.BeginInvoke(count, state.UserDefinedState, ReceiveCallbackCallback, state);
				}
				else
				{
					state.Callback.BeginInvoke(count, new AsyncCallback(ReceiveCallbackCallback), state);
				}
			}
		}

		private static void ReceiveCallbackCallback(IAsyncResult ar)
		{
			var state = (State) ar.AsyncState;
			state.Callback.EndInvoke(ar);
		}

		#endregion
	}
}