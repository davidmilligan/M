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
        public const string Everyone = "Everyone";

        private static readonly object _auctionLock = new object();

        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool IsStarted { get; set; }

        public bool IsActive { get; set; }

        public bool AuctionsEnabled { get; set; }

        public DateTimeOffset Created { get; set; }

        public virtual List<Location> Locations { get; set; }

        public virtual List<Player> Players { get; set; }

        public virtual List<Player> WaitingRoom { get; set; }

        public virtual List<Message> Messages { get; set; }

        public string Owner { get; set; }

        public string Turn { get; set; }

        public int LastRoll1 { get; set; }

        public int LastRoll2 { get; set; }

        public int DoubleCount { get; set; }

        public decimal MoneyOwed { get; set; }

        public string MoneyOwedTo { get; set; }

        public string TurnMessage { get; set; }

        public decimal FreeParking { get; set; }

        public decimal RentAdjustment { get; set; }

        public int AuctionProperty { get; set; }

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
            else if (string.IsNullOrEmpty(Owner))
            {
                Owner = user;
                Players.Add(CreatePlayer(user, connectionId));
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
                Messages.Remove(Messages.OrderBy(t => t.DateTime).First());
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
            TurnMessage = null;
            LastRoll1 = RandomNumberGenerator.GetInt32(5) + 1;
            LastRoll2 = RandomNumberGenerator.GetInt32(5) + 1;
            if (player.IsInJail && LastRoll1 == LastRoll2)
            {
                player.IsInJail = false;
                DoubleCount = -1;
            }
            if (!player.IsInJail)
            {
                if (LastRoll1 == LastRoll2)
                {
                    DoubleCount++;
                    if (DoubleCount == 3)
                    {
                        player.IsInJail = true;
                        player.Position = Locations.FirstOrDefault(t => t.Type == LocationType.Jail)?.Position ?? player.Position;
                        TurnMessage = " sent to jail for 3 doubles in a row";
                        Message(connectionId, TurnMessage);
                        return null;
                    }
                }
                player.MoveBy(LastRoll1 + LastRoll2, this);
            }
            else
            {
                TurnMessage = " remained in jail";
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
            AuctionProperty = 0;
            UpdatePropertiesForOwnership();
            Message(connectionId, $"purchased {location}");
            return null;
        }

        public string DoNotBuy(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (LastRoll1 == 0) { return "You must roll first!"; }

            var location = Locations.FirstOrDefault(t => t.Position == player.Position);
            if (location.Price <= 0) { return "You are not on a property"; }
            if (location.Owner != null) { return $"{location.Owner} already owns this property"; }

            if (AuctionsEnabled)
            {
                AuctionProperty = player.Position;
                Message(connectionId, $"Auction started for {location}");
            }
            else
            {
                AuctionProperty = 0;
            }
            return null;
        }

        public string Bid(string connectionId, decimal amount)
        {
            lock (_auctionLock) //TODO: better bidding race condition handling
            {
                var (error, player) = CheckPlayer(connectionId);
                if (error != null) { return error; }
                if (!AuctionsEnabled || AuctionProperty <= 0) { return "There is nothing for auction"; }
                if (player.Money < amount) { return "You don't have enough money"; }

                if (amount > CurrentBid())
                {
                    foreach (var other in Players)
                    {
                        other.HasBid = false;
                        other.CurrentBid = 0;
                    }
                    player.CurrentBid = amount;
                    Message(connectionId, $"bid {amount:C}");
                }
                else
                {
                    if (amount > 0) { return "Your bid was too low"; }
                    player.CurrentBid = 0;
                }
                player.HasBid = true;
                if (Players.All(t => t.HasBid))
                {
                    var winner = Players.OrderByDescending(t => t.CurrentBid).First();
                    winner.Money -= winner.CurrentBid;
                    var property = Locations.Find(t => t.Position == AuctionProperty);
                    if (property != null)
                    {
                        property.Owner = winner.Name;
                        UpdatePropertiesForOwnership();
                        AuctionProperty = 0;
                        Message(null, $"{winner} won the auction for {property}");
                    }
                    foreach (var other in Players)
                    {
                        other.HasBid = false;
                        other.CurrentBid = 0;
                    }
                }
            }
            return null;
        }

        public decimal CurrentBid() => Players.Where(t => t.HasBid).Max(t => (decimal?)t.CurrentBid) ?? 0M;

        public string ForSale(string connectionId, int property, decimal amount, string to)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }

            var location = Locations.FirstOrDefault(t => t.Position == property);
            if (location.Price <= 0) { return "Not found"; }
            if (location.Owner != player.Name) { return "You don't own this property!"; }
            if (location.IsMortgaged) { return "The property is mortgaged"; }

            location.ForSaleAmount = amount;
            location.ForSaleTo = to;
            return null;
        }

        public string BuyProperty(string connectionId, int property)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }

            var location = Locations.FirstOrDefault(t => t.Position == property);
            if (location.Price <= 0) { return "Not found"; }
            if (location.ForSaleAmount <= 0) { return "The property is not for sale"; }
            if (location.ForSaleTo != null && location.ForSaleTo != player.Name) { return "The property is not for sale to you!"; }
            if (player.Money < location.ForSaleAmount) { return "You don't have enough money"; }

            location.Owner = player.Name;
            player.Money -= location.Price;
            AuctionProperty = 0;
            UpdatePropertiesForOwnership();
            Message(connectionId, $"purchased {location}");
            return null;
        }

        public void UpdatePropertiesForOwnership()
        {
            foreach (var group in Locations.GroupBy(t => t.Group))
            {
                switch (group.First().Type)
                {
                    case LocationType.SpecialProperty:
                        foreach (var ownerGroup in group.GroupBy(t => t.Owner))
                        {
                            if (ownerGroup.Key != null)
                            {
                                foreach (var location in ownerGroup)
                                {
                                    location.Improvements = ownerGroup.Count();
                                }
                            }
                        }
                        break;
                    case LocationType.Property:
                        foreach (var ownerGroup in group.GroupBy(t => t.Owner))
                        {
                            if (ownerGroup.Key != null)
                            {
                                if (ownerGroup.Count() == group.Count())
                                {
                                    foreach (var location in ownerGroup)
                                    {
                                        if (location.Improvements == 0)
                                        {
                                            location.Improvements = 1;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var location in ownerGroup)
                                    {
                                        location.Improvements = 0;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        public string DoNotBuyProperty(string connectionId, int property)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }
            var location = Locations.FirstOrDefault(t => t.Position == property);
            if (location.Price <= 0) { return "Not found"; }
            if (location.ForSaleAmount <= 0) { return "The property is not for sale"; }

            location.ForSaleAmount = 0;
            return null;
        }

        public string Upgrade(string connectionId, int position)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }
            var location = Locations.FirstOrDefault(t => t.Position == position);
            if (location.Owner != player.Name) { return "You don't own this property!"; }
            if (location.Type != LocationType.Property) { return "This is not an upgradeable property"; }
            var locationGroup = Locations.Where(t => t.Group == location.Group);
            if (!locationGroup.All(t => t.Owner == location.Owner)) { return "You don't have a monopoly!"; }
            if (location.Improvements > locationGroup.Min(t => t.Improvements)) { return "You must upgrade houses evenly!"; }
            if (location.Improvements >= location.MaxImprovements(this)) { return "You cannot upgrade this property any more"; }
            if (player.Money < location.UpgradeCost) { return "You do not have enough money to upgrade this property!"; }
            player.Money -= location.UpgradeCost;
            location.Improvements++;
            Message(connectionId, $"upgraded {location}");
            return null;
        }

        public string Mortgage(string connectionId, int position)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }
            var location = Locations.FirstOrDefault(t => t.Position == position);
            if (location.Owner != player.Name) { return "You don't own this property!"; }
            if (location.IsMortgaged) { return "The property is already mortgaged"; }
            player.Money += location.MortgageValue();
            location.IsMortgaged = true;
            return null;
        }

        public string PayMortgage(string connectionId, int position)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }
            var location = Locations.FirstOrDefault(t => t.Position == position);
            if (location.Owner != player.Name) { return "You don't own this property!"; }
            if (!location.IsMortgaged) { return "The property is not mortgaged"; }
            if (player.Money < location.MortgageCost()) { return "You do not have enough money to pay the mortgage!"; }
            player.Money -= location.MortgageCost();
            location.IsMortgaged = false;
            return null;
        }

        public string PayPlayerDebt(string connectionId)
        {
            var (error, player) = CheckPlayer(connectionId);
            if (error != null) { return error; }
            if (player.MoneyOwed <= 0) { return "You don't owe any money"; }
            if (player.Money < player.MoneyOwed) { return "You don't have enough money!"; }
            player.Money -= player.MoneyOwed;
            var owedTo = Players.Find(t => t.Name == player.MoneyOwedTo);
            if (owedTo != null)
            {
                owedTo.Money += player.MoneyOwed;
            }
            else
            {
                FreeParking += player.MoneyOwed;
            }
            player.MoneyOwed = 0M;
            player.MoneyOwedTo = null;
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
            if (MoneyOwedTo == Everyone)
            {
                var toEach = MoneyOwed / (Players.Count - 1);
                foreach (var other in Players.Where(t => t != player))
                {
                    other.Money += toEach;
                }
                Message(connectionId, $"paid {toEach:C} to everyone");
            }
            else
            {
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
            }
            MoneyOwed = 0;
            return null;
        }

        public string GetOutOfJailFree(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (!player.IsInJail) { return "You are not in jail"; }
            if (player.GetOutOfJailFree <= 0) { return "You cannot get out of jail free"; }
            player.IsInJail = false;
            player.GetOutOfJailFree--;
            return null;
        }

        public string EndTurn(string connectionId)
        {
            var (error, player) = CheckTurn(connectionId);
            if (error != null) { return error; }
            if (LastRoll1 == 0 || (LastRoll1 == LastRoll2 && DoubleCount >= 0)) { return "You can't end your turn yet"; }
            if (MoneyOwed > 0) { return "You must pay your debts before ending your turn!"; }
            if (Players.Any(t => t.MoneyOwed > 0M)) { return "Waiting on other players to pay"; }
            if (AuctionsEnabled && AuctionProperty < 0) { return "You must decide if you want to purchase the property"; }
            if (AuctionsEnabled && AuctionProperty > 0) { return "Waiting on auction to finish"; }

            return DoEndTurn(player);
        }

        private string DoEndTurn(Player player)
        {
            var nextPlayer = Players.Find(t => t.Order == (player.Order + 1) % Players.Count);
            if (nextPlayer == null) { return "Server Error: Could not find next player"; }
            Turn = nextPlayer.Name;
            LastRoll1 = 0;
            LastRoll2 = 0;
            MoneyOwed = 0;
            MoneyOwedTo = null;
            DoubleCount = 0;
            AuctionProperty = 0;
            RentAdjustment = 0;
            return null;

        }

        public string Retire(string connectionId)
        {
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player != null)
            {
                if (Turn == player.Name)
                {
                    DoEndTurn(player);
                }
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

        private (string, Player) CheckPlayer(string connectionId)
        {
            if (!IsActive) { return ("Game has already ended!", null); }
            if (!IsStarted) { return ("Game has not started!", null); }
            var player = Players.FirstOrDefault(t => t.ConnectionId == connectionId);
            if (player == null) { return ("Player not found", null); }
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

        public decimal OwnedPropertyValue(string player)
        {
            var sum = 0M;
            foreach (var property in Locations.Where(t => t.Owner == player))
            {
                sum += property.Price + (property.Improvements * property.UpgradeCost);
            }
            return sum;
        }

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
                AuctionsEnabled = true,
                Locations = SetupLocations(Classic()).ToList()
            };
            game.Messages = new List<Message>();
            game.Players = new List<Player>();
            game.WaitingRoom = new List<Player>();
            if (!string.IsNullOrEmpty(owner))
            {
                game.Players.Add(CreatePlayer(owner, connectionId));
            }
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
            yield return new Location { Name = "Go", Icon = "long-arrow-alt-up" };

            yield return new Location { Name = "Mediteranean Avenue", Group = "1", Color = "#4B0082", Price = 60M, UpgradeCost = 50M, Type = LocationType.Property };
            yield return new Location { Name = "Community Chest", Icon = "briefcase", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicCommunityChest().ToList() };
            yield return new Location { Name = "Baltic Avenue", Group = "1", Color = "#4B0082", Price = 60M, UpgradeCost = 50M, Type = LocationType.Property };
            yield return new Location { Name = "Income Tax", Icon = "file-invoice-dollar", Type = LocationType.Tax, Tax = 200M, Rate = 0.1M };
            yield return new Location { Name = "Reading Railroad", Group = "Railroad", Icon = "train", Price = 200M, Type = LocationType.SpecialProperty };
            yield return new Location { Name = "Oriental Avenue", Group = "2", Color = "#F0F8FF", Price = 100M, UpgradeCost = 50M, Type = LocationType.Property };
            yield return new Location { Name = "Chance", Icon = "question", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicChance().ToList() };
            yield return new Location { Name = "Vermont Avenue", Group = "2", Color = "#F0F8FF", Price = 100M, UpgradeCost = 50M, Type = LocationType.Property };
            yield return new Location { Name = "Connecticut Avenue", Group = "2", Color = "#F0F8FF", Price = 120M, UpgradeCost = 50M, Type = LocationType.Property };

            yield return new Location { Name = "Jail / Just Visiting", Icon = "dungeon", Type = LocationType.Jail, Tax = 50M };

            yield return new Location { Name = "St. Charles Place", Group = "3", Color = "#DA70D6", Price = 140M, UpgradeCost = 100M, Type = LocationType.Property };
            yield return new Location { Name = "Electric Company", Group = "Utility", Icon = "lightbulb", Price = 150M, Type = LocationType.SpecialProperty, Rate = 4M };
            yield return new Location { Name = "States Avenue", Group = "3", Color = "#DA70D6", Price = 140M, UpgradeCost = 100M, Type = LocationType.Property };
            yield return new Location { Name = "Virginia Avenue", Group = "3", Color = "#DA70D6", Price = 160M, UpgradeCost = 100M, Type = LocationType.Property };
            yield return new Location { Name = "Pennsylvania  Railroad", Group = "Railroad", Icon = "train", Price = 200M, Type = LocationType.SpecialProperty };
            yield return new Location { Name = "St. James Place", Group = "4", Color = "#FF8C00", Price = 180M, UpgradeCost = 100M, Type = LocationType.Property };
            yield return new Location { Name = "Community Chest", Icon = "briefcase", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicCommunityChest().ToList() };
            yield return new Location { Name = "Tennessee Avenue", Group = "4", Color = "#FF8C00", Price = 180M, UpgradeCost = 100M, Type = LocationType.Property };
            yield return new Location { Name = "New York Avenue", Group = "4", Color = "#FF8C00", Price = 200M, UpgradeCost = 100M, Type = LocationType.Property };

            yield return new Location { Name = "Free Parking", Icon = "parking", Type = LocationType.FreeParking };

            yield return new Location { Name = "Kentucky Avenue", Group = "5", Color = "#B22222", Price = 220M, UpgradeCost = 150M, Type = LocationType.Property };
            yield return new Location { Name = "Chance", Icon = "question", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicChance().ToList() };
            yield return new Location { Name = "Indiana Avenue", Group = "5", Color = "#B22222", Price = 220M, UpgradeCost = 150M, Type = LocationType.Property };
            yield return new Location { Name = "Illinois Avenue", Group = "5", Color = "#B22222", Price = 240M, UpgradeCost = 150M, Type = LocationType.Property };
            yield return new Location { Name = "B & O Railroad", Group = "Railroad", Icon = "train", Price = 200M, Type = LocationType.SpecialProperty };
            yield return new Location { Name = "Atlantic Avenue", Group = "6", Color = "#FFD700", Price = 260M, UpgradeCost = 150M, Type = LocationType.Property };
            yield return new Location { Name = "Ventor Avenue", Group = "6", Color = "#FFD700", Price = 260M, UpgradeCost = 150M, Type = LocationType.Property };
            yield return new Location { Name = "Water Works", Group = "Utility", Icon = "faucet", Price = 150M, Type = LocationType.SpecialProperty, Rate = 4M };
            yield return new Location { Name = "Marvin Gardens", Group = "6", Color = "#FFD700", Price = 280M, UpgradeCost = 150M, Type = LocationType.Property };

            yield return new Location { Name = "Go to Jail", Icon = "hand-point-up", Type = LocationType.GoToJail };

            yield return new Location { Name = "Pacific Avenue", Group = "7", Color = "#228B22", Price = 300M, UpgradeCost = 200M, Type = LocationType.Property };
            yield return new Location { Name = "North Carolina Avenue", Group = "7", Color = "#228B22", Price = 300M, UpgradeCost = 200M, Type = LocationType.Property };
            yield return new Location { Name = "Community Chest", Icon = "briefcase", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicCommunityChest().ToList() };
            yield return new Location { Name = "Pennsylvania Avenue", Group = "7", Color = "#228B22", Price = 320M, UpgradeCost = 200M, Type = LocationType.Property };
            yield return new Location { Name = "Short Line", Group = "Railroad", Icon = "train", Price = 200M, Type = LocationType.SpecialProperty };
            yield return new Location { Name = "Chance", Icon = "question", Type = LocationType.Random, RandomEvents = RandomEvent.ClassicChance().ToList() };
            yield return new Location { Name = "Park Place", Group = "8", Color = "#1E90FF", Price = 350M, UpgradeCost = 200M, Type = LocationType.Property };
            yield return new Location { Name = "Luxury Tax", Icon = "gem", Type = LocationType.Tax, Tax = 75M };
            yield return new Location { Name = "Boardwalk", Group = "8", Color = "#1E90FF", Price = 400M, UpgradeCost = 200M, Type = LocationType.Property };
        }
    }
}
