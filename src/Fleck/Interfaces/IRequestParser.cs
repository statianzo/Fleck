using System;

namespace Fleck.Interfaces
{
  public interface IRequestParser
  {
    bool IsComplete(byte[] bytes);
    WebSocketHttpRequest Parse(byte[] bytes);
  }
}

