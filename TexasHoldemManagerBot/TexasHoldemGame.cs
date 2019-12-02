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
            public uint Bet { get; set; } = 0;


            public Player(string name)
            {
                Name = name;
            }
        }
    }
}
