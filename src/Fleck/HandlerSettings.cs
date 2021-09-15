using System;

namespace Fleck
{
    public class HandlerSettings
    {
        public static readonly HandlerSettings Default = new HandlerSettings();
        
        public int Hybi13MaxMessageSize { get; set; } = Int32.MaxValue;
    }
}