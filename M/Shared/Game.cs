using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace M.Shared
{
    public class Game
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool IsStarted { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset Created { get; set; }

        public virtual List<Location> Locations { get; set; }

        public virtual List<Player> Players { get; set; }

        public virtual List<Player> WaitingRoom { get; set; }

        public string Owner { get; set; }

        public string Turn { get; set; }

        public int LastRoll1 { get; set; }

        public int LastRoll2 { get; set; }

        public decimal FreeParking { get; set; }

        public static Player CreatePlayer(string name, string connectionId) => new Player
        {
            ConnectionId = connectionId,
            Name = name,
            Money = 100_000,
        };

        public string Join(string user, string connectionId)
        {
            var existing = Players.Concat(WaitingRoom).FirstOrDefault(t => t.Name == user);
            if (existing != null)
            {
                existing.ConnectionId = connectionId;
            }
            else
            {
                WaitingRoom.Add(CreatePlayer(user, connectionId));
            }
            return null;
        }

        public string Admit(string connectionId, string name)
        {
            if (IsStarted) { return "Game has already started!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can admit users!"; }
            var player = WaitingRoom.FirstOrDefault(t => t.Name == name);
            if (player == null) { return $"Player not found: {name}"; }

            WaitingRoom.Remove(player);
            player.Order = Players.Count;
            Players.Add(player);
            return null;
        }

        public string Roll(string connectionId)
        {
            if (!IsActive) { return "Game has already ended!"; }
            if (!IsStarted) { return "Game has not started!"; }
            if (LastRoll1 != 0 || LastRoll2 != 0) { return "It's not time to roll"; }
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player == null) { return "Player not found"; }
            if (Turn == player.Name) { return "It's not your turn!"; }

            LastRoll1 = RandomNumberGenerator.GetInt32(5) + 1;
            LastRoll2 = RandomNumberGenerator.GetInt32(5) + 1;
            player.Position += LastRoll1 + LastRoll2;
            return null;
        }

        public string Start(string connectionId)
        {
            if (IsStarted) { return "Game has already started!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can start game!"; }
            if (Players.Count < 2) { return "You must have at least 2 players!"; }

            IsStarted = true;
            Turn = Owner;
            return null;
        }

        public string End(string connectionId)
        {
            if (!IsActive) { return "Game has already ended!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can end game!"; }

            IsActive = false;
            return null;
        }

        public string SetIcon(string connectionId, string icon)
        {
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player == null) { return "Player not found"; }
            if (Players.Any(t => t.Icon == icon)) { return "Another player is already using that icon"!; }

            player.Icon = icon;
            return null;
        }

        public string UserFromConnectionId(string id) => Players.Concat(WaitingRoom).FirstOrDefault(t => t.ConnectionId == id)?.Name;

        public static Game New(string owner, string name, string connectionId)
        {
            int i = 0;
            Location Location(Location location)
            {
                location.Id = Guid.NewGuid();
                location.Position = i;
                i++;
                return location;
            }
            var game = new Game()
            {
                Id = Guid.NewGuid(),
                Created = DateTimeOffset.Now,
                Name = name,
                Owner = owner,
                IsActive = true,
                IsStarted = false,
                LastRoll1 = 0,
                LastRoll2 = 2,
                Turn = owner,
                Locations = new List<Location>
                {
                    Location(new Location()),
                    Location(new Property { Name = "Square 1", Price = 100 }),
                    Location(new Property { Name = "Square 2", Price = 100 }),
                }
            };
            game.Players = new List<Player>();
            game.WaitingRoom = new List<Player>();
            game.Players.Add(CreatePlayer(owner, connectionId));
            return game;
        }
    }
}
