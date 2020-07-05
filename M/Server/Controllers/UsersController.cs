using M.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using M.Server.Models;
using M.Server.Data;

namespace M.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private ApplicationDbContext Context { get; }

        public UsersController(ApplicationDbContext context)
        {
            Context = context;
        }

        [HttpGet]
        public IEnumerable<string> Get() => Context.Users.Select(t => t.UserName);
    }
}
