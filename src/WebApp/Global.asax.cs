using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Fleck;

namespace WebApp
{
    public class Global : System.Web.HttpApplication
    {
        Action<string> _log;

        public Global()
        {
            _log = text => Application["log"] += text + "\r\n";
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://localhost:8181");
            _log.Invoke("Server starting...");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _log.Invoke("Open!");
                    allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    _log.Invoke("Close!");
                    allSockets.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    _log.Invoke(message);
                    allSockets.ToList().ForEach(s => s.Send("Echo: " + message));
                };
            });            
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}