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
                    if (game.Locations[(i + player.Position) % game.Locations.Count].Group == MoveTargetGroup)
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
                    if (PayTarget == PayTarget.Bank)
                    {
                        game.MoneyOwed = -Amount;
                    }
                    else
                    {
                        game.MoneyOwed = -Amount * game.Players.Count - 1;
                        game.MoneyOwedTo = Game.Everyone;
                    }
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
            yield return new RandomEvent { Message = "Advance to the nearest railroad and pay owner twice the rental fee", MoveTarget = MoveTarget.Type, MoveTargetGroup = "Railroad", RentAdjustment = 2M };
            yield return new RandomEvent { Message = "Take a walk on the Board Walk", MoveTarget = MoveTarget.Absolute, MoveAmount = 39 };
            yield return new RandomEvent { Message = "Take a ride on the Reading Railroad", MoveTarget = MoveTarget.Absolute, MoveAmount = 5 };
            yield return new RandomEvent { Message = "Go back 3 spaces", MoveTarget = MoveTarget.Relative, MoveAmount = -3 };
            yield return new RandomEvent { Message = "Pay Poor Tax of $15", PayTarget = PayTarget.Bank, Amount = -15M };
            yield return new RandomEvent { Message = "Bank pays you dividend of $50", PayTarget = PayTarget.Bank, Amount = 50M };
            yield return new RandomEvent { Message = "Advance to the nearest utility and pay owner twice the rental fee", MoveTarget = MoveTarget.Type, MoveTargetGroup = "Utility", RentAdjustment = 2M };
        }

        public static IEnumerable<RandomEvent> ClassicCommunityChest()
        {
            yield return new RandomEvent { Message = "Grand Opera Opening, Collect $50 from each player for opening night seats", Amount = 50M, PayTarget = PayTarget.Everyone };
            yield return new RandomEvent { Message = "Receive for Services", PayTarget = PayTarget.Bank, Amount = 25M };
            yield return new RandomEvent { Message = "Advance to Go", MoveTarget = MoveTarget.Absolute, MoveAmount = 0 };
            yield return new RandomEvent { Message = "You have won second prize in a beauty contest, collect $10", PayTarget = PayTarget.Bank, Amount = 10M };
            yield return new RandomEvent { Message = "From sale of stock you get $45", PayTarget = PayTarget.Bank, Amount = 45M };
            yield return new RandomEvent { Message = "You inherit $100", PayTarget = PayTarget.Bank, Amount = 100M };
            yield return new RandomEvent { Message = "Go Directly to Jail", MoveTarget = MoveTarget.Jail };
            yield return new RandomEvent { Message = "Bank Error in your favor, collect $200", PayTarget = PayTarget.Bank, Amount = 200M };
            yield return new RandomEvent { Message = "Pay School Tax of $150", PayTarget = PayTarget.Bank, Amount = -150M };
            yield return new RandomEvent { Message = "Pay Hospital $100", PayTarget = PayTarget.Bank, Amount = -100M };
            yield return new RandomEvent { Message = "Get Out Of Jail Free", SpecialEvent = SpecialEvent.GetOutOfJailFree };
            yield return new RandomEvent { Message = "Income Tax Refund, collect $20", PayTarget = PayTarget.Bank, Amount = 20M };
            yield return new RandomEvent { Message = "You are assessed for street repairs ($40 for each house, $200 for each hotel)", SpecialEvent = SpecialEvent.MakeRepairs, Amount = 40M };
            yield return new RandomEvent { Message = "Xmas fund matures, collect $100", PayTarget = PayTarget.Bank, Amount = 100M };
            yield return new RandomEvent { Message = "Life insurance matures, collect $100", PayTarget = PayTarget.Bank, Amount = 100M };
            yield return new RandomEvent { Message = "Pay Doctor's fee $50", PayTarget = PayTarget.Bank, Amount = 50M };
        }
    }
}
