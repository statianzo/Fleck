using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Fleck
{
  public class RequestParser
  {
    const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
                                   @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+";
    
    public WebSocketHttpRequest Parse(ArraySegment<byte> bytes)
    {
      var request = new WebSocketHttpRequest();
      
      var body = Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
      
      var regex = new Regex(pattern, RegexOptions.IgnoreCase);
      Match match = regex.Match(body);
      
      request.Method = match.Groups["method"].Value;
      request.Path = match.Groups["path"].Value;
      
      var fields = match.Groups["field_name"].Captures;
      var values = match.Groups["field_value"].Captures;
      for (var i = 0; i < fields.Count; i++) {
        var name = fields[i].ToString();
        var value = values[i].ToString();
        request.Headers[name] = value;
      }
      
      return request;
    }
  }
}

