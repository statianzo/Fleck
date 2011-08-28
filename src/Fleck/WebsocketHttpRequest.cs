using System;
using System.Collections.Generic;

namespace Fleck
{
  public class WebSocketHttpRequest
  {
    IDictionary<string,string> headers = new Dictionary<string,string>();

    public string Method { get; set; }

    public string Path { get; set; }
    
    public IDictionary<string,string> Headers {
      get {
        return headers;
      }
    }
    
  }
}

