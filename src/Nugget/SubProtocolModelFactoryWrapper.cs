using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;

namespace Nugget
{
    public class SubProtocolModelFactoryWrapper
    {
        public object Factory { get; private set; }
        public SubProtocolModelFactoryWrapper(object factory)
        {
            Factory = factory;
        }

        public object Create(string data, WebSocketConnection connection)
        {
            var create = Factory.GetType().GetMethods().Single(x => x.Name == "Create");

            try 
            { 
                return create.Invoke(Factory, new object[] { data, connection }); 
            }
            catch (Exception e)
            {
                Log.Error("exception thrown in " + Factory.GetType().Name + ".Create: " + e.Message);
                return null;
            }
            
            
        }

        public bool IsValid(object model)
        {
            var create = Factory.GetType().GetMethods().Single(x => x.Name == "IsValid");
            
            try 
            {
                return (bool)create.Invoke(Factory, new object[] { model });
            }
            catch (Exception e)
            {
                Log.Error("exception thrown in " + Factory.GetType().Name + ".IsValid: " + e.Message);
                return false;
            }            
            
        }
    }
}
