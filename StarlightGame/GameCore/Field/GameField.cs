using System;
using System.Runtime.Serialization;
using StarlightEngine.Graphics.Math;

namespace StarlightGame.GameCore.Field
{
    [Serializable()]
    public class GameField : ISerializable
    {
        #region Private Members
        Random m_rng;

        StarSystem[] m_starSystems;
        #endregion

        #region Constructors
        public GameField(int numStars)
        {
            m_rng = new Random();

            // generate numStars StarSystems, make sure they're not too close together
            Console.WriteLine("Generating {0} stars...", numStars);
            m_starSystems = new StarSystem[numStars];
            float minDistance = (float)(2.0f / (Math.PI * Math.Sqrt(numStars))) / 10.0f;
            for (int i = 0; i < numStars; i++)
            {
                FVec2 randomLocation = new FVec2(0.0f, 0.0f);
                bool acceptable = true;

                do
                {
                    randomLocation = new FVec2(((float)m_rng.NextDouble() * 2) - 1.0f, ((float)m_rng.NextDouble() * 2) - 1.0f);
                    for (int j = 0; j < i; j++)
                    {
                        acceptable &= (randomLocation - m_starSystems[j].Location).Length() > minDistance;
                    }
                }
                while (!acceptable);

                m_starSystems[i] = new StarSystem("", randomLocation);
                Console.Write(".");
            }
            Console.WriteLine("\nDone!");
        }
        #endregion

        #region Properties
        public StarSystem[] Stars
        {
            get
            {
                return m_starSystems;
            }
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("m_starSystems", m_starSystems);
        }

        // Deserialization constructor
        public GameField(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_rng = new Random();

            m_starSystems = (StarSystem[])serializationInfo.GetValue("m_starSystems", typeof(StarSystem[]));
        }
        #endregion
    }
}