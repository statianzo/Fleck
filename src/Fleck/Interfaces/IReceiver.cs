using System;

namespace Fleck.Interfaces
{
    public interface IReceiver
    {
        event Action OnError;
        event Action<string> OnMessage;
        
        void Receive();
    }
}

