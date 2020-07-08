using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace M.Shared
{
    public class Player
    {
        public Guid Id { get; set; }

        public int Order { get; set; }

        public string Name { get; set; }

        public int Position { get; set; }

        public string ConnectionId { get; set; }

        public decimal Money { get; set; }

        public string Icon { get; set; }

        public bool IsInJail { get; set; }

        public int GetOutOfJailFree { get; set; }

        public decimal MoneyOwed { get; set; }

        public string MoneyOwedTo { get; set; }

        public decimal CurrentBid { get; set; }

        public bool HasBid { get; set; }

        public override string ToString() => Name?.Split('@').FirstOrDefault();

        public void MoveBy(int delta, Game game)
        {
            MoveTo(Position + delta, game);
        }

        public void MoveForwardTo(int position, Game game)
        {
            if (Position > position)
            {
                position += game.Locations.Count;
            }
            MoveTo(position, game);
        }

        public void MoveTo(int position, Game game)
        {
            Position = position;
            if (Position >= game.Locations.Count)
            {
                Money += Game.PassGoMoney;
                game.Message(null, $"{this} passed go");
            }
            Position = (Position + game.Locations.Count) % game.Locations.Count;
            if (game.Locations.Find(t => t.Position == Position) is Location location)
            {
                location.PlayerLandedOn(game, this);
                if (!string.IsNullOrEmpty(game.TurnMessage))
                {
                    game.Message(ConnectionId, game.TurnMessage);
                }
            }
        }
    }
}
