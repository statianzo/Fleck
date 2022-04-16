using System;
using System.Threading.Tasks;

namespace Fleck
{
    /// <summary>
    /// 获取自动连接脚本
    /// </summary>
    /// <param name="host">主机名称</param>
    /// <param name="path">请求路径(去除/auto/)</param>
    /// <returns></returns>
    public delegate string GetAutoScriptHandler(string host, string path, string encode);
    public interface IWebSocketConnection
    {
        Action OnOpen { get; set; }
        Action OnClose { get; set; }
        Action<string> OnMessage { get; set; }
        Action<byte[]> OnBinary { get; set; }
        Action<byte[]> OnPing { get; set; }
        Action<byte[]> OnPong { get; set; }
        Action<Exception> OnError { get; set; }
        Task Send(string message);
        Task Send(byte[] message);
        Task SendPing(byte[] message);
        Task SendPong(byte[] message);
        void Close();
        void Close(int code);
        IWebSocketConnectionInfo ConnectionInfo { get; }
        bool IsAvailable { get; }
        /// <summary>
        /// 获取自动连接脚本
        /// </summary>
        event GetAutoScriptHandler GetAutoScript;
    }
}
