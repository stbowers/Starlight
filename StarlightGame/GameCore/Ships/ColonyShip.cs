using System;

namespace StarlightGame.GameCore.Ships
{
    public class ColonyShip : IShip
    {
        Empire m_owner;

        public ColonyShip(Empire owner)
        {
            m_owner = owner;
        }

        public Empire Owner
        {
            get
            {
                return m_owner;
            }
        }
    }
}