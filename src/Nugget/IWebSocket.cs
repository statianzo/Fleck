using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nugget
{
    public interface IWebSocket
    {
        void Disconnected();
        void Connected(ClientHandshake handshake);
    }
}
