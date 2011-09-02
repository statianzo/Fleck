using System;
using Fleck.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Fleck
{
    public class ResponseBuilderFactory : IResponseBuilderFactory
    {
        private IList<IResponseBuilder> builders = new List<IResponseBuilder>();
        
        public void Register(IResponseBuilder builder)
        {
            builders.Add(builder);
        }
     
        public IResponseBuilder Resolve(WebSocketHttpRequest request)
        {
            return builders.FirstOrDefault(b => b.CanHandle(request));
        }
    }
}

