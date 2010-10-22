using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;

namespace Nugget
{
    class SubProtocolModelFactoryStore
    {
        private Dictionary<string, object> instances = new Dictionary<string, object>();

        public void Store<TModel>(ISubProtocolModelFactory<TModel> factory, string subprotocol)
        {
            instances.Add(subprotocol, factory);
        }

        public object Get(string subprotocol)
        {
            return instances[subprotocol];
        }
    }
}
