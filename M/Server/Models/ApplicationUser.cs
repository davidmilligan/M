using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace M.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public string DefaultIcon { get; set; }
        public decimal TotalMoneyWon { get; set; }
    }
}
