using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;

using StarlightGame.GameCore.Field;
using StarlightEngine.Math;
using StarlightGame.GameCore.Projects;

namespace StarlightGame.GameCore
{
    [Serializable()]
    public class GameState : ISerializable
    {
        #region Private Members
        Random m_rng;

        GameField m_field;
        Empire m_playerEmpire;

        List<(IProject, ProjectAttribute)> m_availableProjects = new List<(IProject, ProjectAttribute)>();

        static GameState m_gameState;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new game state - initialized for a new game
        /// </summary>
        public GameState(string playerName, FVec4 playerPrimaryColor, FVec4 playerSecondaryColor)
        {
            m_rng = RNG.GetRNG();
            m_field = new GameField();
            m_playerEmpire = new Empire(playerName, playerPrimaryColor, playerSecondaryColor);

            Assembly searchAssembly = Assembly.GetAssembly(typeof(GameState));
            m_availableProjects.AddRange(
                from type in searchAssembly.GetTypes()
                where Attribute.IsDefined(type, typeof(ProjectAttribute))
                select ((IProject)type.GetConstructor(new Type[] { }).Invoke(null), (ProjectAttribute)Attribute.GetCustomAttribute(type, typeof(ProjectAttribute)))
            );
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

        public List<(IProject, ProjectAttribute)> AvailableProjects
        {
            get
            {
                return m_availableProjects;
            }
        }

        public static GameState State
        {
            get
            {
                return m_gameState;
            }
            set
            {
                m_gameState = value;
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