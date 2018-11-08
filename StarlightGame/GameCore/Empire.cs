using System;
using System.Collections.Generic;
using System.Linq;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightGame.GameCore.Ships;

namespace StarlightGame.GameCore
{
    public class Empire
    {
        string m_name;
        List<StarSystem> m_ownedSystems = new List<StarSystem>();
        List<IShip> m_ownedShips = new List<IShip>();

        public Empire(string name)
        {
            m_name = name;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public List<IShip> Ships
        {
            get
            {
                return m_ownedShips;
            }
        }

        public void ClaimSystem(StarSystem system)
        {
            m_ownedSystems.Add(system);
        }
    }
}