using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Nugget
{
    public class WebSocketWrapper
    {
        private object webSocket;

        Dictionary<Type, MethodInfo> methodMap;

        public WebSocketWrapper(object ws)
        {
            webSocket = ws;

            // get all the methods with the name Incoming - the method defined on the IReceivingWebSocket<> interface
            // the interface is generic and can be implemented with more than one type parameter (e.g. class C : I<string>, I<int>)
            // we need them all
            var methods = webSocket.GetType().GetMethods().Where(x => x.Name == "Incoming");
            
            // save the methods in a dictionary to make it simpler to find the methods
            methodMap = methods.ToDictionary(x => x.GetParameters().First().ParameterType);
        }

        // finds the right method on the enclosed websocket, based on the type of the model (the object created by the factory)
        public void Incoming(object model)
        {
            if (model == null)
            {
                // find methods that accept nullable types
                var matches = methodMap.Where(x => x.Key.IsClass || x.Key.IsInterface);
                
                // did we find any?
                if (matches.Count() > 0)
                {
                    // call the first match with the empty model
                    matches.First().Value.Invoke(webSocket, new object[] { model });
                    
                    // does more than one method accept a nullable type 
                    if (matches.Count() > 1)
                    {
                        // log it
                        Log.Warn(
                            String.Format("more than one matching method found for empty(null) model on {0} ({1} called)",
                                webSocket.GetType().Name, 
                                matches.First().Value.ToString())
                            );
                    }
                }
                return;
            }

            // look for the method that takes an argument of the type of the model
            // e.g. void Incoming(string model) - if the model is of the type string
            var match = methodMap.SingleOrDefault(x => x.Key == model.GetType()).Value;
            
            // look for the methods that takes an argument of the type that the model is derived from
            // e.g. void Incoming(I model) - if model implements 'I'
            //   or void Incoming(C model) - if model is a subclass of 'C'
            var subMatches = methodMap.Where(x => model.GetType().IsSubclassOf(x.Key) || 
                                                  model.GetType().GetInterfaces().Contains(x.Key))
                                      .Select(x => x.Value);

            // if we found a perfect match
            if (match != null)
            {
                // invoke the method and worry no more
                try { match.Invoke(webSocket, new object[] { model });}
                catch (Exception e)
                {
                    Log.Error("exception thrown in " + webSocket.GetType().Name + ".Incoming: " + e.Message);
                }
            }
            else
            {
                // have we got other methods that will accept the model?
                if (subMatches.Count() > 0)
                {
                    // invoke the method first of them
                    try { subMatches.First().Invoke(webSocket, new object[] { model }); }
                    catch (Exception e)
                    {
                        Log.Error("exception thrown in " + webSocket.GetType().Name + ".Incoming: " + e.Message);
                    }
                    
                    // if we have more than one match
                    if (subMatches.Count() > 1)
                    {
                        // log
                        Log.Warn(String.Format("more than one matching method found for model of type : {0} on {1} ({2} called)", model.GetType().Name, webSocket.GetType().Name, subMatches.First().ToString()));
                    }
                }
                else // nobody wants the model
                {
                    Log.Warn(String.Format("{0} can't handle model of type {1}", webSocket.GetType().Name, model.GetType().Name));
                }
            }
        }

        public void Connected(ClientHandshake handshake)
        {
            ((IWebSocket)webSocket).Connected(handshake);
        }

        public void Disconnected()
        {
            ((IWebSocket)webSocket).Disconnected();
        }
    }
}
