using System;

namespace Fleck.Interfaces
{
  public interface IRequestParser
  {
    bool IsComplete(ArraySegment<byte> bytes);
    WebSocketHttpRequest Parse(ArraySegment<byte> bytes);
  }
}

