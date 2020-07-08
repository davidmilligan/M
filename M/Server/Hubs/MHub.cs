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
            .Include(t => t.Messages)
            .Include(t => t.Players)
            .Include(t => t.WaitingRoom)
            .Include(t => t.Locations)
                .ThenInclude(t => t.RandomEvents);

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
        public Task<Game> Message(Guid id, string value) => SendUpdate(id, t => t.Message(Context.ConnectionId, value, true));
        public Task<Game> Start(Guid id) => SendUpdate(id, t => t.Start(Context.ConnectionId));
        public Task<Game> Roll(Guid id) => SendUpdate(id, t => t.Roll(Context.ConnectionId));
        public Task<Game> Pay(Guid id) => SendUpdate(id, t => t.Pay(Context.ConnectionId));
        public Task<Game> PayPlayerDebt(Guid id) => SendUpdate(id, t => t.PayPlayerDebt(Context.ConnectionId));
        public Task<Game> GetOutOfJailFree(Guid id) => SendUpdate(id, t => t.GetOutOfJailFree(Context.ConnectionId));
        public Task<Game> Buy(Guid id) => SendUpdate(id, t => t.Buy(Context.ConnectionId));
        public Task<Game> Bid(Guid id, decimal amount) => SendUpdate(id, t => t.Bid(Context.ConnectionId, amount));
        public Task<Game> DoNotBuy(Guid id) => SendUpdate(id, t => t.DoNotBuy(Context.ConnectionId));
        public Task<Game> ForSale(Guid id, int position, decimal amount, string to) => SendUpdate(id, t => t.ForSale(Context.ConnectionId, position, amount, to));
        public Task<Game> BuyProperty(Guid id, int position) => SendUpdate(id, t => t.BuyProperty(Context.ConnectionId, position));
        public Task<Game> DoNotBuyProperty(Guid id, int position) => SendUpdate(id, t => t.DoNotBuyProperty(Context.ConnectionId, position));
        public Task<Game> Upgrade(Guid id, int position) => SendUpdate(id, t => t.Upgrade(Context.ConnectionId, position));
        public Task<Game> Mortgage(Guid id, int position) => SendUpdate(id, t => t.Mortgage(Context.ConnectionId, position));
        public Task<Game> PayMortgage(Guid id, int position) => SendUpdate(id, t => t.PayMortgage(Context.ConnectionId, position));
        public Task<Game> EndTurn(Guid id) => SendUpdate(id, t => t.EndTurn(Context.ConnectionId));

        public async Task<Game> Retire(Guid id)
        {
            var game = await SendUpdate(id, t => t.Retire(Context.ConnectionId));
            if (game != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString());
            }
            return game;
        }

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
            var game = await Games.FirstOrDefaultAsync(t => t.IsActive && (t.Players.Any(p => p.ConnectionId == id) || t.WaitingRoom.Any(p => p.ConnectionId == id)));
            if (game != null)
            {
                var player = game.Players.Concat(game.WaitingRoom).FirstOrDefault(t => t.ConnectionId == id);
                player.ConnectionId = null;
                await SendUpdate(game.Id, t => t.Message(Context.ConnectionId, $"{player} is offline", false));
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
