using System;
using System.Runtime.Serialization;

namespace StarlightGame.GameCore.Ships
{
    [Serializable]
    /// <summary>
    /// Interop class which can be deserialized from the server's ship, and then converted into the proper ship type
    /// </summary>
    public class BasicShip : IShip
    {
        Empire m_owner;
        string m_name;
        string m_type;

        public BasicShip(Empire owner, string name)
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
                return m_type;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", m_name);
            info.AddValue("Type", Type);
        }

        public BasicShip(SerializationInfo info, StreamingContext context)
        {
            m_name = (string)info.GetValue("Name", typeof(string));
            m_type = (string)info.GetValue("Type", typeof(string));
        }

        public IShip ToProperShipType(){
            switch (m_type){
                case "Colony Ship":
                    return new ColonyShip(m_owner, m_name);
                    break;
                case "Science Ship":
                    return new ScienceShip(m_owner, m_name);
                    break;
            }
            return this;
        }
    }
}