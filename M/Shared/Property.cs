using System;
using System.Collections.Generic;
using System.Text;

namespace M.Shared
{
    public class Property : Location
    {
        public string Owner { get; set; }
        public bool IsMortgaged { get; set; }
        public decimal Price { get; set; }
    }
}
