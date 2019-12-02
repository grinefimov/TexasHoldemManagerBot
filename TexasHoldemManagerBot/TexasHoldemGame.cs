using System.Collections.Generic;

namespace TexasHoldemManagerBot
{
    internal class TexasHoldemGame
    {
        public List<Player> Players { get; set; }
        public uint Pot { get; set; } = 0;


        public TexasHoldemGame()
        {
            Players = new List<Player>();
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

            public void Reset()
            {
                Bankroll = 0;
                TotalBet = 0;
                State = PlayerState.Ready;
            }

            public enum PlayerState { Ready, Call, Bet, Fold };
        }
    }
}
