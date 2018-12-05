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
        string[] m_neighborNames;
        Empire m_owner;
        string m_ownerName;
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
            info.AddValue("Name", m_name);
            info.AddValue("Location", m_location.Data);
            string[] neighbors = m_neighbors.ConvertAll((star) => star.Name).ToArray();
            info.AddValue("Neighbors", neighbors);
            info.AddValue("Owner", m_owner?.Name);
            info.AddValue("Colonized", m_colonized);
        }

        // Deserialization constructor
        public StarSystem(SerializationInfo info, StreamingContext streamingContext)
        {
            m_name = (string)info.GetValue("Name", typeof(string));
            m_location = new FVec2((float[])info.GetValue("Location", typeof(float[])));
            m_neighborNames = (string[])info.GetValue("Neighbors", typeof(string[]));
            m_ownerName = (string)info.GetValue("Owner", typeof(string));
            m_colonized = (bool)info.GetValue("Colonized", typeof(bool));
        }

        /// <summary>
        /// Rebuilds references to systems, empires, and ships after deserializing a system (seperate method since everything must be deserialized before calling)
        /// </summary>
        public void Rebuild(List<StarSystem> systems, List<Empire> empires)
        {
            foreach (string neighbor in m_neighborNames)
            {
                m_neighbors.Add(systems.Find((system) => system.Name == neighbor));
            }

            m_owner = empires.Find((empire) => empire.Name == m_ownerName);
        }
        #endregion
    }
}