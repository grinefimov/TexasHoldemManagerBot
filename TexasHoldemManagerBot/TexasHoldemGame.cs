using System.Collections.Generic;

namespace TexasHoldemManagerBot
{
    internal class TexasHoldemGame
    {
        public List<Player> Players { get; set; }
        public uint Pot { get; set; } = 0;
        public uint SmallBlind { get; set; } = 0;
        public uint BigBlind { get; set; } = 0;


        public TexasHoldemGame()
        {
            Players = new List<Player>();
        }

        public void SetBlinds(uint amount)
        {
            SmallBlind = amount;
            BigBlind = amount * 2;
        }

        public void Restart()
        {
            Players = new List<Player>();
            Pot = 0;
            SmallBlind = 0;
            BigBlind = 0;
        }

        public class Player
        {
            public string Name { get; set; }
            public uint Bankroll { get; set; } = 0;
            public uint TotalBet { get; set; } = 0;
            public PlayerState State { get; set; } = PlayerState.Ready;
            
            public Player(string name)
            {
                Name = name;
            }

            public void Ready()
            {
                TotalBet = 0;
                State = PlayerState.Ready;
            }

            public void Call(uint amount)
            {
                Bankroll -= amount;
                TotalBet += amount;
                State = PlayerState.Call;
            }

            public void Bet(uint amount)
            {
                Bankroll -= amount;
                TotalBet += amount;
                State = PlayerState.Bet;
            }

            public void Fold()
            {
                State = PlayerState.Fold;
            }

            public enum PlayerState { Ready, Call, Bet, Fold };
        }
    }
}
