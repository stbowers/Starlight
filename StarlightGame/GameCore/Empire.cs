using System;

namespace StarlightGame.GameCore
{
    public class Empire
    {
        string m_name;

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
    }
}