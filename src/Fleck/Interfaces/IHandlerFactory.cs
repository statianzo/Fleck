using System;

namespace Fleck.Interfaces
{
    public interface IHandlerFactory 
    {
        IHandler BuildHandler(byte[] data, Action<string> onMessage, Action onClose);
    }
    
}

