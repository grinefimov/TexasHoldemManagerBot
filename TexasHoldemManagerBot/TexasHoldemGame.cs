using System.Collections.Generic;
using System.Linq;

namespace TexasHoldemManagerBot
{
    internal class TexasHoldemGame
    {
        public List<Player> Players { get; set; }
        public uint Pot { get; set; }
        public uint SmallBlind { get; set; }
        public uint BigBlind { get; set; }


        public TexasHoldemGame()
        {
            Players = new List<Player>();
        }

        public void SetBlinds(uint smallBlind)
        {
            SmallBlind = smallBlind;
            BigBlind = smallBlind * 2;
        }

        public void Restart()
        {
            Players = new List<Player>();
            Pot = 0;
            SmallBlind = 0;
            BigBlind = 0;
        }

        public bool AddPlayer(string name)
        {
            if (Players.FirstOrDefault(p => p.Name == name) != null) return false;
            Players.Add(new Player(name, this));
            return true;
        }

        public void AddChips(int chipsToAdd)
        {
            foreach (var p in Players)
            {
                if ((int)p.Bankroll + chipsToAdd >= 0)
                    p.Bankroll += (uint)chipsToAdd;
                else
                    p.Bankroll = 0;
            }
        }

        public bool RemovePlayer(string name)
        {
            if (Players.FirstOrDefault(p => p.Name == name) == null) return false;
            Players.Remove(Players.FirstOrDefault(p => p.Name == name));
            return true;
        }

        public class Player
        {
            public string Name { get; set; }
            public uint Bankroll { get; set; }
            public uint TotalBet { get; set; }
            public PlayerState State { get; set; } = PlayerState.Ready;
            private readonly TexasHoldemGame _game;

            public enum PlayerState { Ready, Call, Bet, Fold };

            public Player(string name, TexasHoldemGame game)
            {
                Name = name;
                _game = game;
            }

            public void Ready()
            {
                TotalBet = 0;
                State = PlayerState.Ready;
            }

            public uint Call()
            {
                var maxBet = _game.Players.Select(p => p.TotalBet).Max();
                var chipsToCall = maxBet - TotalBet;
                Bankroll -= chipsToCall;
                TotalBet += chipsToCall;
                State = PlayerState.Call;
                _game.Pot += chipsToCall;
                return chipsToCall;
            }

            public bool Bet(uint amount)
            {
                if ((int) _game.Pot + amount < 0 || (int) Bankroll - amount < 0 || (int) TotalBet + amount < 0 ||
                    amount == 0)
                    return false;
                Bankroll -= amount;
                TotalBet += amount;
                State = PlayerState.Bet;
                _game.Pot += amount;
                return true;
            }

            public void Fold()
            {
                State = PlayerState.Fold;
            }

            public bool AddChips(int amount)
            {
                if ((int) Bankroll + amount < 0) return false;
                Bankroll += (uint)amount;
                return false;
            }

            public bool Win(uint chipsToWin)
            {
                if ((int) _game.Pot - chipsToWin < 0) return false;
                Bankroll += chipsToWin;
                _game.Pot -= chipsToWin;
                _game.Players.ForEach(p => p.Ready());
                return true;
            }
        }
    }
}