using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace M.Shared
{
    public class Player
    {
        public Guid Id { get; set; }

        public int Order { get; set; }

        public string Name { get; set; }

        public int Position { get; set; }

        public string ConnectionId { get; set; }

        public decimal Money { get; set; }

        public string Icon { get; set; }

        public bool IsInJail { get; set; }

        public override string ToString() => Name?.Split('@').FirstOrDefault();
    }
}
