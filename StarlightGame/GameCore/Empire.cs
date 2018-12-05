using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightGame.GameCore.Ships;
using StarlightEngine.Math;

namespace StarlightGame.GameCore
{
    [Serializable]
    public class Empire : ISerializable
    {
        string m_name;
        List<StarSystem> m_ownedSystems = new List<StarSystem>();
        List<IShip> m_ownedShips = new List<IShip>();

        FVec4 m_primaryColor;
        FVec4 m_secondaryColor;

        public Empire(string name, FVec4 primaryColor, FVec4 secondaryColor)
        {
            m_name = name;
            m_primaryColor = primaryColor;
            m_secondaryColor = secondaryColor;
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

        public FVec4 PrimaryColor
        {
            get
            {
                return m_primaryColor;
            }
        }

        public FVec4 SecondaryColor
        {
            get
            {
                return m_secondaryColor;
            }
        }

        public void ClaimSystem(StarSystem system)
        {
            m_ownedSystems.Add(system);
        }

        #region Serialization Functions
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("Name", Name);
            serializationInfo.AddValue("PrimaryColor", PrimaryColor.Data);
            serializationInfo.AddValue("SecondaryColor", SecondaryColor.Data);
        }

        public Empire(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_name = serializationInfo.GetString("Name");
            m_primaryColor = new FVec4((float[])serializationInfo.GetValue("PrimaryColor", typeof(float[])));
            m_secondaryColor = new FVec4((float[])serializationInfo.GetValue("SecondaryColor", typeof(float[])));
        }
        #endregion
    }
}