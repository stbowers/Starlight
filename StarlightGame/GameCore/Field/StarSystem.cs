using System;
using System.Runtime.Serialization;
using StarlightEngine.Graphics.Math;

namespace StarlightGame.GameCore.Field
{
    [Serializable()]
    public class StarSystem : ISerializable
    {
        #region Private Members
        string m_name;
        FVec2 m_location;
        StarSystem[] m_neighbors;
        #endregion

        #region Constructors
        /// <summary>
        /// Make a new star system with the given name
        /// </summary>
        public StarSystem(string name, FVec2 location)
        {
            m_name = name;
            m_location = location;
        }
        #endregion

        #region Properties
        public FVec2 Location
        {
            get
            {
                return m_location;
            }
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo info, StreamingContext streamingContext)
        {

        }

        // Deserialization constructor
        public StarSystem(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {

        }
        #endregion
    }
}