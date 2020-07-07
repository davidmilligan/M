using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace M.Shared
{
    public class Game
    {
        public const int MaxPlayers = 16;
        public const int StartMoney = 1_500;
        public const int PassGoMoney = 200;
        public const int MaxHouses = 5;
        public const int MaxMessages = 100;

        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool IsStarted { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset Created { get; set; }

        public virtual List<Location> Locations { get; set; }

        public virtual List<Player> Players { get; set; }

        public virtual List<Player> WaitingRoom { get; set; }

        public virtual List<Message> Messages { get; set; }

        public string Owner { get; set; }

        public string Turn { get; set; }

        public int LastRoll1 { get; set; }

        public int LastRoll2 { get; set; }

        public decimal MoneyOwed { get; set; }

        public string MoneyOwedTo { get; set; }

        public string TurnMessage { get; set; }

        public decimal FreeParking { get; set; }

        public static Player CreatePlayer(string name, string connectionId) => new Player
        {
            ConnectionId = connectionId,
            Name = name,
            Money = StartMoney,
        };

        public string Join(string user, string connectionId)
        {
            var existing = Players.Concat(WaitingRoom).FirstOrDefault(t => t.Name == user);
            if (existing != null)
            {
                existing.ConnectionId = connectionId;
                Message(connectionId, $"re-joined the game");
            }
            else
            {
                if (Players.Count >= MaxPlayers) { return $"Game already has maximum number of players"; }
                WaitingRoom.Add(CreatePlayer(user, connectionId));
                Message(connectionId, $"requested to join the game");
            }
            return null;
        }

        public string Admit(string connectionId, string name)
        {
            if (IsStarted) { return "Game has already started!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can admit users!"; }
            if (Players.Count >= MaxPlayers) { return $"Maximum number of players is {MaxPlayers}"; }
            var player = WaitingRoom.FirstOrDefault(t => t.Name == name);
            if (player == null) { return $"Player not found: {name}"; }

            WaitingRoom.Remove(player);
            player.Order = Players.Count;
            Players.Add(player);
            Message(null, $"{player} was admitted");
            return null;
        }

        public string Message(string connectionId, string value) => Message(connectionId, value, false);

        public string Message(string connectionId, string value, bool isChat)
        {
            if (Messages.Count >= MaxMessages)
            {
                Messages.Remove(Messages.OrderBy(t => t.DateTime).Last());
            }
            Messages.Add(new Message
            {
                DateTime = DateTimeOffset.Now,
                From = UserFromConnectionId(connectionId),
                Value = value,
                IsChat = isChat,
            });
            return null;
        }

        public string Roll(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (LastRoll1 != 0 && LastRoll1 != LastRoll2) { return "It's not time to roll"; }

            LastRoll1 = RandomNumberGenerator.GetInt32(5) + 1;
            LastRoll2 = RandomNumberGenerator.GetInt32(5) + 1;
            if (player.IsInJail && LastRoll1 == LastRoll2)
            {
                player.IsInJail = false;
            }
            if (!player.IsInJail)
            {
                player.Position += LastRoll1 + LastRoll2;
                if (player.Position >= Locations.Count)
                {
                    player.Money += PassGoMoney;
                    Message(null, $"{player} passed go");
                }
                player.Position %= Locations.Count;
                if (Locations.Find(t => t.Position == player.Position) is Location location)
                {
                    location.PlayerLandedOn(this, player);
                    if (!string.IsNullOrEmpty(TurnMessage))
                    {
                        Message(null, TurnMessage);
                    }
                }
            }
            return null;
        }

        public string Buy(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (LastRoll1 == 0) { return "You must roll first!"; }

            var location = Locations.FirstOrDefault(t => t.Position == player.Position);
            if (location.Price <= 0) { return "You are not on a property"; }
            if (location.Owner != null) { return $"{location.Owner} already owns this property"; }
            if (player.Money < location.Price) { return "You do not have enough money to buy this property!"; }
            location.Owner = player.Name;
            player.Money -= location.Price;
            location.Bought(this);
            Message(connectionId, $"purchased {location}");
            return null;
        }

        public string Pay(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (player.IsInJail)
            {
                var bond = Locations.FirstOrDefault(t => t.Type == LocationType.Jail)?.Tax ?? 50M;
                if (player.Money < bond) { return "You don't have enough money!"; }
                player.Money -= bond;
                FreeParking += bond;
                player.IsInJail = false;
                Message(connectionId, $"paid {bond:C} jail bond");
                return null;
            }
            if (MoneyOwed <= 0) { return "You don't owe any money"; }
            if (player.Money < MoneyOwed) { return "You don't have enough money!"; }

            player.Money -= MoneyOwed;
            var playerOwed = MoneyOwedTo != null ? Players.Find(t => t.Name == MoneyOwedTo) : null;
            if (playerOwed != null)
            {
                playerOwed.Money += MoneyOwed;
                Message(connectionId, $"paid {MoneyOwed:C} to {playerOwed}");
            }
            else
            {
                FreeParking += MoneyOwed;
                Message(connectionId, $"paid {MoneyOwed:C}");
            }
            MoneyOwed = 0;
            return null;
        }

        public string EndTurn(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (LastRoll1 == 0 || LastRoll1 == LastRoll2) { return "You can't end your turn yet"; }
            if (MoneyOwed > 0) { return "You must pay your debts before ending your turn!"; }

            var nextPlayer = Players.Find(t => t.Order == (player.Order + 1) % Players.Count);
            if (nextPlayer == null) { return "Server Error: Could not find next player"; }
            Turn = nextPlayer.Name;
            LastRoll1 = 0;
            LastRoll2 = 0;
            MoneyOwed = 0;
            MoneyOwedTo = null;
            return null;
        }

        public string Retire(string connectionId)
        {
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player != null)
            {
                Players.Remove(player);
                int i = 0;
                foreach (var other in Players.OrderBy(t => t.Order))
                {
                    other.Order = i;
                    i++;
                }
                foreach (var property in Locations)
                {
                    if (property.Owner == player.Name)
                    {
                        property.Owner = null;
                        property.IsMortgaged = false;
                    }
                }
                Message(null, $"{player} left the game");
            }
            return null;
        }

        private (string, Player) CheckTurn(string connectionId)
        {
            if (!IsActive) { return ("Game has already ended!", null); }
            if (!IsStarted) { return ("Game has not started!", null); }
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player == null) { return ("Player not found", null); }
            if (Turn != player.Name) { return ("It's not your turn!", null); }
            return (null, player);
        }

        public string Start(string connectionId)
        {
            if (IsStarted) { return "Game has already started!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can start game!"; }
            if (Players.Count < 2) { return "You must have at least 2 players!"; }

            IsStarted = true;
            Turn = Owner;
            LastRoll1 = 0;
            Message(null, "Game started!");
            return null;
        }

        public string End(string connectionId)
        {
            if (!IsActive) { return "Game has already ended!"; }
            if (UserFromConnectionId(connectionId) != Owner) { return "Only game owner can end game!"; }

            Message(null, "Game ended!");
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
                Locations = SetupLocations(Classic()).ToList()
            };
            game.Messages = new List<Message>();
            game.Players = new List<Player>();
            game.WaitingRoom = new List<Player>();
            game.Players.Add(CreatePlayer(owner, connectionId));
            return game;
        }

        private static IEnumerable<Location> SetupLocations(IEnumerable<Location> locations)
        {
            int i = 0;
            foreach (var location in locations)
            {
                location.Position = i;
                i++;
                yield return location;
            }
        }

        private static IEnumerable<Location> Classic()
        {
            yield return new Location { Name = "Go" };
            yield return new Location { Name = "Mediteranean Avenue", Group = "1", Color = "Plum", Price = 60M };
            yield return new Location { Name = "Community Chest", Type = LocationType.Random };
            yield return new Location { Name = "Baltic Avenue", Group = "1", Color = "Plum", Price = 60M };
            yield return new Location { Name = "Income Tax", Type = LocationType.Tax, Tax = 200M, Rate = 0.1M };
            yield return new Location { Name = "Reading Railroad", Group = "Railroad", Type = LocationType.SpecialProperty, Price = 200M };
            yield return new Location { Name = "Oriental Avenue", Group = "2", Color = "LightCyan", Price = 100M };
            yield return new Location { Name = "Chance", Type = LocationType.Random };
            yield return new Location { Name = "Vermont Avenue", Group = "2", Color = "LightCyan", Price = 100M };
            yield return new Location { Name = "Connecticut Avenue", Group = "2", Color = "LightCyan", Price = 120M };
            yield return new Location { Name = "Jail / Just Visiting", Type = LocationType.Jail };
            yield return new Location { Name = "St. Charles Place", Group = "3", Color = "Orchid", Price = 140M };
            yield return new Location { Name = "Electric Company", Group = "Utilities", Price = 150M, Rate = 4M };
            yield return new Location { Name = "States Avenue", Group = "3", Color = "Orchid", Price = 140M };
            yield return new Location { Name = "Virgine Avenue", Group = "3", Color = "Orchid", Price = 160M };
            yield return new Location { Name = "Pennsylvania  Railroad", Group = "Railroad", Type = LocationType.SpecialProperty, Price = 200M };
            yield return new Location { Name = "St. James Place", Group = "4", Color = "Orange", Price = 180M };
            yield return new Location { Name = "Community Chest", Type = LocationType.Random };
            yield return new Location { Name = "Tennessee Avenue", Group = "4", Color = "Orange", Price = 180M };
            yield return new Location { Name = "New York Avenue", Group = "4", Color = "Orange", Price = 200M };
            yield return new Location { Name = "Free Parking", Type = LocationType.FreeParking };
            yield return new Location { Name = "Kentucky Avenue", Group = "5", Color = "Orange", Price = 220M };
            yield return new Location { Name = "Chance", Type = LocationType.Random };
            yield return new Location { Name = "Indiana Avenue", Group = "5", Color = "Orange", Price = 220M };
            yield return new Location { Name = "Illinois Avenue", Group = "5", Color = "Orange", Price = 240M };
            yield return new Location { Name = "B & O Railroad", Group = "Railroad", Type = LocationType.SpecialProperty, Price = 200M };
            yield return new Location { Name = "Atlantic Avenue", Group = "6", Color = "Yellow", Price = 260M };
            yield return new Location { Name = "Ventor Avenue", Group = "6", Color = "Yellow", Price = 260M };
            yield return new Location { Name = "Water Works", Group = "Utilities", Price = 150M, Rate = 4M };
            yield return new Location { Name = "Marvin Gardens", Group = "6", Color = "Yellow", Price = 280M };
            yield return new Location { Name = "Go to Jail", Type = LocationType.GoToJail };
            yield return new Location { Name = "Pacific Avenue", Group = "7", Color = "Green", Price = 300M };
            yield return new Location { Name = "North Caroline Avenue", Group = "7", Color = "Green", Price = 300M };
            yield return new Location { Name = "Community Chest", Type = LocationType.Random };
            yield return new Location { Name = "Pennsylvania Avenue", Group = "7", Color = "Green", Price = 320M };
            yield return new Location { Name = "Short Line", Group = "Railroad", Type = LocationType.SpecialProperty, Price = 200M };
            yield return new Location { Name = "Chance", Type = LocationType.Random };
            yield return new Location { Name = "Park Place", Group = "8", Color = "Blue", Price = 320M };
            yield return new Location { Name = "Luxury Tax", Type = LocationType.Tax, Tax = 75M };
            yield return new Location { Name = "Boardwalk", Group = "8", Color = "Blue", Price = 320M };
        }
    }
}
