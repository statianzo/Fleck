using System;

namespace Fleck.Interfaces
{
    public interface ISender
    {
        event Action OnError;
        
        void SendText(string text);
    }
}

