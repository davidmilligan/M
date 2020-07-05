using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise.DataGrid;
using M.Server.Data;
using M.Server.Models;
using M.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace M.Server.Hubs
{
    [Authorize]
    public class MHub : Hub
    {
        private ApplicationDbContext DbContext { get; }

        private IQueryable<Game> Games => DbContext.Games
            .Include(t => t.Players)
            .Include(t => t.WaitingRoom)
            .Include(t => t.Locations);

        public MHub(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<Game> NewGame(string owner, string name)
        {
            var newGame = Game.New(owner, name, Context.ConnectionId);
            DbContext.Games.Add(newGame);
            await DbContext.SaveChangesAsync();
            await Groups.AddToGroupAsync(Context.ConnectionId, newGame.Id.ToString());
            return newGame;
        }

        public async Task<Game> Join(Guid id, string requestor)
        {
            var game = await SendUpdate(id, t => t.Join(requestor, Context.ConnectionId));
            if (game != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
            }
            return game;
        }

        public Task<Game> Admit(Guid id, string user) => SendUpdate(id, t => t.Admit(Context.ConnectionId, user));

        public Task<Game> Start(Guid id) => SendUpdate(id, t => t.Start(Context.ConnectionId));

        public Task<Game> Roll(Guid id) => SendUpdate(id, t => t.Roll(Context.ConnectionId));

        public Task<Game> End(Guid id) => SendUpdate(id, t => t.End(Context.ConnectionId));

        public Task<Game> SetIcon(Guid id, string icon) => SendUpdate(id, t => t.SetIcon(Context.ConnectionId, icon));

        private Task<Game> GetGame(Guid id) => Games.FirstOrDefaultAsync(t => t.Id == id);

        private async Task<Game> SendUpdate(Guid id, Func<Game, string> func)
        {
            var game = await GetGame(id);
            if (game != null)
            {
                var error = func(game);
                if (!string.IsNullOrEmpty(error))
                {
                    await Clients.Caller.SendAsync("Error", error);
                }
                else
                {
                    await DbContext.SaveChangesAsync();
                    await Clients.Group(game.Id.ToString()).SendAsync("Update", game);
                }
            }
            return game;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var id = Context.ConnectionId;
            foreach (var player in await Games.Where(t => t.IsActive).SelectMany(t => t.Players.Concat(t.WaitingRoom)).Where(t => t.ConnectionId == id).ToListAsync())
            {
                player.ConnectionId = null;
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
