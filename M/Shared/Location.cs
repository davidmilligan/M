using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace M.Shared
{
    public enum LocationType { Property, Tax, SpecialProperty, FreeParking, Random, Jail, GoToJail }

    public class Location
    {
        public Guid Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public int Improvements { get; set; }
        public string Owner { get; set; }
        public bool IsMortgaged { get; set; }
        public decimal Price { get; set; }
        public decimal RentOverride { get; set; }
        public decimal UpgradeCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Rate { get; set; }
        public LocationType Type { get; set; }
        public string TurnMessage { get; set; }
        public decimal ForSaleAmount { get; set; }
        public string ForSaleTo { get; set; }
        public virtual List<RandomEvent> RandomEvents { get; set; }

        public void PlayerLandedOn(Game game, Player player)
        {
            if (game != null && player != null)
            {
                var sb = new StringBuilder().Append(" landed on ").Append(Name);
                if (Price > 0 && Improvements > 0)
                {
                    sb.Append(" (").Append(ImprovementDescription(Improvements)).Append(")");
                }
                if (!string.IsNullOrEmpty(TurnMessage))
                {
                    sb.AppendLine().Append(TurnMessage);
                }
                game.TurnMessage = sb.ToString();
                if (Type == LocationType.FreeParking)
                {
                    game.Message(player.ConnectionId, $"collected {game.FreeParking:C} from {Name}");
                    player.Money += game.FreeParking;
                    game.FreeParking = 0;
                }
                else if (Type == LocationType.GoToJail)
                {
                    player.IsInJail = true;
                    player.Position = game.Locations.FirstOrDefault(t => t.Type == LocationType.Jail)?.Position ?? player.Position;
                }
                else if (Type == LocationType.Random)
                {
                    if (RandomEvents?.Any() == true)
                    {
                        var randomEvent = RandomEvents[RandomNumberGenerator.GetInt32(RandomEvents.Count)];
                        randomEvent.Execute(game, player);
                        sb.AppendLine().Append("\"").Append(randomEvent.Message).Append("\"");
                    }
                    else
                    {
                        var amount = (RandomNumberGenerator.GetInt32(10) - 3) * 10M;
                        if (amount < 0)
                        {
                            game.MoneyOwed = -amount;
                            game.MoneyOwedTo = null;
                            sb.AppendLine().Append($"must pay {-amount:C}");
                        }
                        else
                        {
                            amount += 10M;
                            player.Money += amount;
                            sb.AppendLine().Append($"recieve {amount:C}");
                        }
                    }
                    game.TurnMessage = sb.ToString();
                }
                if (!IsMortgaged && player.Name != Owner && (Owner != null || Type == LocationType.Tax))
                {
                    game.MoneyOwed = Rent(Improvements, game, player);
                    game.MoneyOwedTo = Owner;
                }
            }
        }

        public void Bought(Game game)
        {
            var group = game.Locations.Where(t => t.Group == Group);
            var owned = group.Where(t => t.Owner == Owner);
            if (Type == LocationType.SpecialProperty)
            {
                var improvements = owned.Count() - 1;
                foreach (var property in owned)
                {
                    property.Improvements = improvements;
                }
            }
            else if (Type == LocationType.Property)
            {
                if (owned.Count() == group.Count())
                {
                    foreach (var property in owned)
                    {
                        property.Improvements = 1;
                    }
                }
            }
        }

        public decimal Rent(int improvements) => Rent(improvements, null, null);

        public decimal Rent(int improvements, Game game, Player player) => Type switch
        {
            LocationType.Property => Price * 0.1M * (1 << improvements),
            LocationType.Tax when Rate <= 0 => Tax,
            LocationType.Tax when Rate > 0 => Math.Min(Tax, (player?.Money * Rate) ?? Tax),
            LocationType.SpecialProperty when Rate > 0 => ((game?.LastRoll1 + game?.LastRoll2) ?? 1) * Rate * (improvements + 1),
            LocationType.SpecialProperty when Rate <= 0 => Price / 4M * (improvements + 1),
            _ => 0,
        };

        public string ImprovementDescription(int i) => Type switch
        {
            LocationType.Property => i switch { 1 => "Monopoly", 2 => "1 House", Game.MaxHouses + 1 => "Hotel", _ => $"{i - 1} Houses" },
            LocationType.SpecialProperty => $"{i + 1} are owned",
            _ => null
        };

        public int MaxImprovements(Game game) => Type switch
        {
            LocationType.Property => Game.MaxHouses + 1,
            LocationType.SpecialProperty => game?.Locations.Count(t => t.Type == LocationType.SpecialProperty && t.Group == Group) - 1 ?? 0,
            _ => 0
        };

        public decimal MortgageValue() => Price / 2.0M;

        public decimal MortgageCost() => MortgageValue() * 1.1M;

        public override string ToString() => Name;
    }
}
