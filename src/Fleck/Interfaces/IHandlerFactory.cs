using System;

namespace Fleck
{
    public interface IHandlerFactory 
    {
        IHandler BuildHandler(byte[] data, Action<string> onMessage, Action onClose);
    }
    
}

