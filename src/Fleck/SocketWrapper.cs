using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fleck
{
	public class SocketWrapper : ISocket
	{
		private readonly Socket _socket;
    private Stream _stream; 

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
      if(_socket.Connected)  
        _stream = new NetworkStream(_socket);
		}
  
    public void Authenticate(X509Certificate2 certificate, Action callback, Action<Exception> error) 
    {
      var ssl = new SslStream(_stream, false);
      _stream = ssl;
      Func<AsyncCallback, object,IAsyncResult> begin = (cb, s) => ssl.BeginAuthenticateAsServer(certificate,false,System.Security.Authentication.SslProtocols.Tls,false, cb, s);

     var task = Task.Factory.FromAsync(begin, ssl.EndAuthenticateAsServer, null);
       task.ContinueWith(t => {
        callback();
       }, TaskContinuationOptions.NotOnFaulted);
       task.ContinueWith(t => error(t.Exception),
       TaskContinuationOptions.OnlyOnFaulted);
      
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
      return _stream.BeginRead(buffers[0].Array,buffers[0].Offset, buffers[0].Count, callback, state);
		}

		public int EndReceive(IAsyncResult asyncResult)
		{
      return _stream.EndRead(asyncResult);
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
      _stream.Dispose(); 
			_socket.Dispose();
		}

		public void Close()
		{
      _stream.Close();   
			_socket.Close();
		}

		public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback,
		                              object state)
		{
      return _stream.BeginWrite(buffers[0].Array,buffers[0].Offset, buffers[0].Count, callback, state); 
		}

		public int EndSend(IAsyncResult asyncResult)
		{
      _stream.EndWrite(asyncResult);
      return 0;
		}
	}
}