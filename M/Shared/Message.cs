using System;
using System.Collections.Generic;
using System.Text;

namespace M.Shared
{
    public class Message
    {
        public Guid Id { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string From { get; set; }
        public string Value { get; set; }
    }
}
