using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Microsoft.Practices.Unity;

namespace Nugget
{
    /// <summary>
    /// Class for registering and instantiating WebSocketClients
    /// </summary>
    class WebSocketFactory
    {
        private Dictionary<string, Type> types = new Dictionary<string, Type>();
        private UnityContainer container = new UnityContainer();
        
        /// <summary>
        /// Register a new web socket client
        /// </summary>
        /// <typeparam name="T">The web socket client type</typeparam>
        /// <param name="path">The path that the client should respond to</param>
        public void Register<T>(string path) where T : IWebSocket
        {
            if (!types.ContainsKey(path))
            {
                types[path] = typeof(T);
                container.RegisterType<T>();
            }
            else
            {
                throw new Exception("Path: '"+path+"' has already been set");
            }
        }

        public void Register(Type t, string path)
        {
            if (!types.ContainsKey(path))
            {
                types[path] = t;
                container.RegisterType(t);
            }
            else
            {
                throw new Exception("Path: '" + path + "' has already been set");
            }
        }

        /// <summary>
        /// Instantiate a new client
        /// </summary>
        /// <param name="path">The path the client was registered at</param>
        /// <returns>The instantiated WebSocketClient</returns>
        public WebSocketConnection Create(string path)
        {
            if (types.ContainsKey(path))
            {
                try
                {
                    var ws = container.Resolve(types[path]);
                    var wsc = new WebSocketConnection(new WebSocketWrapper(ws));
                    var sws = (ASendingWebSocket)ws;
                    sws.Connection = wsc;

                    return wsc;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
