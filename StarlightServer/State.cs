using System;
using System.Collections.Generic;

namespace StarlightServer
{
    /// <summary>
    /// The server's representation of the game's state
    /// </summary>
    public class GameState
    {
        public GameField Field;
        public List<Empire> Empires;
        public int Turn;
    }

    public class GameField
    {
        public Quadrant[] Quadrants;
    }

    public class Empire
    {
        public string Name;
        public byte[] PrimaryColor;
        public byte[] SecondaryColor;
    }

    public class Quadrant
    {

    }
}