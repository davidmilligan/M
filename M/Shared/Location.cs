using System;
using System.Collections.Generic;
using System.Text;

namespace M.Shared
{
    public class Location
    {
        public Guid Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}
