using System;
using System.Runtime.Serialization;

namespace StarlightGame.GameCore.Ships
{
    [Serializable]
    public class ScienceShip : IShip
    {
        Empire m_owner;
        string m_name;

        public ScienceShip(Empire owner, string name)
        {
            m_owner = owner;
        }

        public Empire GetOwner()
        {
            return m_owner;
        }

        public void SetOwner(Empire empire)
        {
            m_owner = empire;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public string Type
        {
            get
            {
                return "Science Ship";
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", m_name);
            info.AddValue("Type", Type);
        }

        public ScienceShip(SerializationInfo info, StreamingContext context)
        {
            m_name = (string)info.GetValue("Name", typeof(string));
        }
    }
}