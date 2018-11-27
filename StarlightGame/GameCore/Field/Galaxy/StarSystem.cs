using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using StarlightEngine.Math;
using StarlightGame.GameCore.Projects;
using StarlightGame.GameCore.Ships;

namespace StarlightGame.GameCore.Field.Galaxy
{
    [Serializable()]
    public class StarSystem : ISerializable
    {
        #region Private Members
        string m_name;
        FVec2 m_location;
        List<StarSystem> m_neighbors = new List<StarSystem>();
        Empire m_owner;
        bool m_colonized;
        IProject m_currentProject;
        int m_projectTurnsLeft;
        List<IShip> m_ships = new List<IShip>();
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

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public StarSystem[] Neighbors
        {
            get
            {
                return m_neighbors.ToArray();
            }
        }

        public Empire Owner
        {
            get
            {
                return m_owner;
            }
            set
            {
                m_owner = value;
            }
        }

        public bool Colonized
        {
            get
            {
                return m_colonized;
            }
            set
            {
                m_colonized = value;
            }
        }

        public List<IShip> Ships
        {
            get
            {
                return m_ships;
            }
        }

        public IProject CurrentProject
        {
            get
            {
                return m_currentProject;
            }
            set
            {
                m_currentProject = value;
            }
        }
        #endregion

        #region Public methods
        public void AddNeighbor(StarSystem neighbor)
        {
            m_neighbors.Add(neighbor);
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