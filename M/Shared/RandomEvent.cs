using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace M.Shared
{
    public enum PayTarget { None, Bank, Everyone }
    public enum MoveTarget { None, Absolute, Relative, Jail, Type }
    public enum SpecialEvent { None, GetOutOfJailFree, MakeRepairs }

    public class RandomEvent
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
        public PayTarget PayTarget { get; set; }
        public MoveTarget MoveTarget { get; set; }
        public string MoveTargetGroup { get; set; }
        public int MoveAmount { get; set; }
        public decimal RentAdjustment { get; set; }
        public SpecialEvent SpecialEvent { get; set; }
        public string Icon { get; set; }

        public void Execute(Game game, Player player)
        {
            if (SpecialEvent == SpecialEvent.GetOutOfJailFree)
            {
                player.GetOutOfJailFree++;
            }
            else if (SpecialEvent == SpecialEvent.MakeRepairs)
            {
                var improvementCount = game.Locations.Where(t => t.Owner == player.Name && t.Type == LocationType.Property).Sum(t => (int?)t.Improvements) ?? 0;
                game.MoneyOwed = improvementCount * Amount;
                game.MoneyOwedTo = null;
            }
            if (MoveTarget == MoveTarget.Absolute)
            {
                player.MoveForwardTo(MoveAmount, game);
            }
            else if (MoveTarget == MoveTarget.Relative)
            {
                player.MoveBy(MoveAmount, game);
            }
            else if (MoveTarget == MoveTarget.Jail)
            {
                player.IsInJail = true;
                player.Position = game.Locations.FirstOrDefault(t => t.Type == LocationType.Jail)?.Position ?? player.Position;
            }
            else if (MoveTarget == MoveTarget.Type)
            {
                int i;
                for (i = 1; i < game.Locations.Count; i++)
                {
                    if (game.Locations[i].Group == MoveTargetGroup)
                    {
                        break;
                    }
                }
                player.MoveBy(i, game);
            }
            if (PayTarget != PayTarget.None)
            {
                if (Amount > 0)
                {
                    if (PayTarget == PayTarget.Bank)
                    {
                        player.Money += Amount;
                    }
                    else
                    {
                        foreach (var other in game.Players)
                        {
                            if (other.Id != player.Id)
                            {
                                other.MoneyOwed = Amount;
                                other.MoneyOwedTo = player.Name;
                            }
                        }
                    }
                }
                else
                {
                    game.MoneyOwed = -Amount;
                    //TODO: pay to everyone
                }
            }
            game.RentAdjustment = RentAdjustment;
        }

        public static IEnumerable<RandomEvent> ClassicChance()
        {
            yield return new RandomEvent { Message = "Advance to Go", MoveTarget = MoveTarget.Absolute, MoveAmount = 0 };
            yield return new RandomEvent { Message = "Advance to the nearest railroad and pay owner twice the rental fee", MoveTarget = MoveTarget.Type, MoveTargetGroup = "Railroad", RentAdjustment = 2M };
            yield return new RandomEvent { Message = "Get Out Of Jail Free", SpecialEvent = SpecialEvent.GetOutOfJailFree };
            yield return new RandomEvent { Message = "Make General Repairs On All Your Property ($25 for each house, $125 for each hotel)", SpecialEvent = SpecialEvent.MakeRepairs, Amount = 25M };
            yield return new RandomEvent { Message = "Advance to Illinois Ave.", MoveTarget = MoveTarget.Absolute, MoveAmount = 24 };
            yield return new RandomEvent { Message = "Go Directly to Jail", MoveTarget = MoveTarget.Jail };
            yield return new RandomEvent { Message = "Your Building and Loan Matures", Amount = 150M, PayTarget = PayTarget.Bank };
            yield return new RandomEvent { Message = "You have been elected chairman of the board. Pay each player $50", Amount = -50M, PayTarget = PayTarget.Everyone };
            yield return new RandomEvent { Message = "Advance to St. Charles Place", MoveTarget = MoveTarget.Absolute, MoveAmount = 11 };
        }

        public static IEnumerable<RandomEvent> ClassicCommunityChest()
        {
            yield return new RandomEvent { Message = "Grand Opera Opening, Collect $50 from each player for opening night seats", Amount = 50M, PayTarget = PayTarget.Everyone };
        }
    }
}
