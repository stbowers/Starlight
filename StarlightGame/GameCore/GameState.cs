using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;

using StarlightGame.GameCore.Field;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightEngine.Math;
using StarlightGame.GameCore.Projects;

using StarlightEngine.Events;

namespace StarlightGame.GameCore
{
    [Serializable()]
    public class GameState : ISerializable
    {
        #region Private Members
        Random m_rng;

        GameField m_field;
        Empire m_playerEmpire;

        int m_turn;

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

            // create field
            m_field = new GameField();

            // create player empire
            m_playerEmpire = new Empire(playerName, playerPrimaryColor, playerSecondaryColor);

            // find a star for the player empire to start in (Quadrant 1)
            int x = m_rng.Next() % 4;
            int y = m_rng.Next() % 4;
            StarSystem system = m_field.Quadrants[0][x, y];
            while (system == null || system.Neighbors.Length == 0)
            {
                x = m_rng.Next() % 4;
                y = m_rng.Next() % 4;
                system = m_field.Quadrants[0][x, y];
            }
            system.Owner = m_playerEmpire;
            system.Colonized = true;
            Console.WriteLine("Player start: {0}", system.Name);

            // search for and add new instances of any projects
            Assembly searchAssembly = Assembly.GetAssembly(typeof(GameState));
            m_availableProjects.AddRange(
                from type in searchAssembly.GetTypes()
                where Attribute.IsDefined(type, typeof(ProjectAttribute))
                select ((IProject)type.GetConstructor(new Type[] { }).Invoke(null), (ProjectAttribute)Attribute.GetCustomAttribute(type, typeof(ProjectAttribute)))
            );
        }
        #endregion

        #region Public Methods
        public void NextTurn()
        {
            m_turn++;
            EventManager.StaticEventManager.Notify(GameEvent.NextTurnID, this, new GameEvent(), .5f);
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

        public int Turn
        {
            get
            {
                return m_turn;
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