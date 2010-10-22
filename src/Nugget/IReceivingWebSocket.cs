using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nugget
{
    public interface IReceivingWebSocket<T>
    {
        void Incoming(T data);
    }
}
