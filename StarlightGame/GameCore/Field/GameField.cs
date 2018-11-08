using System;
using System.Runtime.Serialization;
using StarlightEngine.Math;
using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.GameCore.Field
{
    [Serializable()]
    public class GameField : ISerializable
    {
        #region Private Members
        Random m_rng;

        Quadrant[] m_quadrants = new Quadrant[4];
        #endregion

        #region Constructors
        public GameField()
        {
            // Generate quadrants
            for (int i = 0; i < 4; i++)
            {
                m_quadrants[i] = new Quadrant(Shape.Spiral2, i);
            }
        }
        #endregion

        #region Properties
        public Quadrant[] Quadrants
        {
            get
            {
                return m_quadrants;
            }
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("m_quadrants", m_quadrants);
        }

        // Deserialization constructor
        public GameField(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_rng = RNG.GetRNG();

            m_quadrants = (Quadrant[])serializationInfo.GetValue("m_quadrants", typeof(Quadrant[]));
        }
        #endregion
    }
}