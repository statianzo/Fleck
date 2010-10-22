using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nugget
{
    public interface ISubProtocolModelFactory<TModel>
    {
        TModel Create(string data, WebSocketConnection connection);
        bool IsValid(TModel model);
    }
}
