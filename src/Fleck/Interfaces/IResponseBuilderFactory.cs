using System;
using Fleck;
using Fleck.Interfaces;

namespace Fleck.Interfaces
{
    public interface IResponseBuilderFactory
    {
        void Register(IResponseBuilder builder);
        IResponseBuilder Resolve(WebSocketHttpRequest request);
    }
}   