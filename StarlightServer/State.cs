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
        public float[] PrimaryColor;
        public float[] SecondaryColor;
        public List<Ship> Ships;
    }

    public class Quadrant
    {
        public Star[,] Stars;
    }

    public class Star
    {
        public string Name;
        public float[] Location;
        public string[] Neighbors;
        public string Owner;
        public bool Colonized;
        public string Project;
        public int ProjectTurnsLeft;
        public List<(string, int)> Ships;
    }

    public class Ship
    {
        public string Name;
        public string Type;
    }

    public class NextTurnData
    {
        public GameState GameState;
        public Empire Empire;
    }
}