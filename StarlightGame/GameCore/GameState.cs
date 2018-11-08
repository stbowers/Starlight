using System;
using System.Runtime.Serialization;

using StarlightGame.GameCore.Field;
using StarlightEngine.Math;

namespace StarlightGame.GameCore
{
    [Serializable()]
    public class GameState : ISerializable
    {
        #region Private Members
        Random m_rng;

        GameField m_field;
        Empire m_playerEmpire;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new game state - initialized for a new game
        /// </summary>
        public GameState(string playerName)
        {
            m_rng = RNG.GetRNG();
            m_field = new GameField();
            m_playerEmpire = new Empire(playerName);
        }
        #endregion

        #region Properties
        public GameField Field
        {
            get
            {
                return m_field;
            }
        }

        public Empire PlayerEmpire
        {
            get
            {
                return m_playerEmpire;
            }
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("m_field", m_field);
        }

        // Deserialization constructor
        public GameState(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_rng = new Random();

            m_field = (GameField)serializationInfo.GetValue("m_field", typeof(GameField));
        }
        #endregion
    }
}