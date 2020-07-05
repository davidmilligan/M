using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using M.Server.Data;
using M.Server.Models;
using M.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace M.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class GamesController : ControllerBase
    {
        private ApplicationDbContext Context { get; }

        private IQueryable<Game> Games => Context.Games
            .Include(t => t.Players)
            .Include(t => t.WaitingRoom)
            .Include(t => t.Locations);

        public GamesController(ApplicationDbContext context)
        {
            Context = context;
        }

        [HttpGet]
        public IEnumerable<Game> Get() => Games.Where(t => !t.IsStarted);
    }
}
