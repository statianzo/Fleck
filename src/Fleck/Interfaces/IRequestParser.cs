namespace Fleck
{
  public interface IRequestParser
  {
    bool IsComplete(byte[] bytes);
    WebSocketHttpRequest Parse(byte[] bytes);
    WebSocketHttpRequest Parse(byte[] bytes, string scheme);
  }
}

