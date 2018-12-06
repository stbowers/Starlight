using System;
using System.Linq;
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
        string m_currentProjectID;
        int m_projectTurnsLeft;
        List<(Empire, int)> m_ships = new List<(Empire, int)>();
        List<(string, int)> m_deserializedShips;
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
                List<IShip> ships = (
                    from pair in m_ships
                    let empire = pair.Item1
                    let shipIndex = pair.Item2
                    select empire.Ships[shipIndex]
                ).ToList();
                return ships;
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

        public int ProjectTurnsLeft
        {
            get
            {
                return m_projectTurnsLeft;
            }
            set
            {
                m_projectTurnsLeft = value;
            }
        }
        #endregion

        #region Public methods
        public void AddNeighbor(StarSystem neighbor)
        {
            m_neighbors.Add(neighbor);
        }

        public void ProcessTurn()
        {
            if (m_currentProject != null)
            {
                m_projectTurnsLeft--;
                Console.WriteLine("{0} turns left: {1}", m_currentProject.Description, m_projectTurnsLeft);
                if (m_projectTurnsLeft == 0)
                {
                    m_currentProject.FinishProject(GameState.State.PlayerEmpire, this);
                    m_currentProject = null;
                }
            }
        }

        public void AddShip(IShip ship)
        {
            m_ships.Add((ship.GetOwner(), ship.GetOwner().Ships.IndexOf(ship)));
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
            info.AddValue("Project", m_currentProject?.ID);
            info.AddValue("ProjectTurnsLeft", m_projectTurnsLeft);
            List<(string, int)> serializedShips = (
                from pair in m_ships
                select (pair.Item1.Name, pair.Item2)
            ).ToList();
            info.AddValue("Ships", serializedShips);
        }

        // Deserialization constructor
        public StarSystem(SerializationInfo info, StreamingContext streamingContext)
        {
            m_name = (string)info.GetValue("Name", typeof(string));
            m_location = new FVec2((float[])info.GetValue("Location", typeof(float[])));
            m_neighborNames = (string[])info.GetValue("Neighbors", typeof(string[]));
            m_ownerName = (string)info.GetValue("Owner", typeof(string));
            m_colonized = (bool)info.GetValue("Colonized", typeof(bool));
            m_currentProjectID = (string)info.GetValue("Project", typeof(string));
            m_projectTurnsLeft = (int)info.GetValue("ProjectTurnsLeft", typeof(int));
            m_deserializedShips = (List<(string, int)>)info.GetValue("Ships", typeof(List<(string, int)>));
        }

        /// <summary>
        /// Rebuilds references to systems, empires, and ships after deserializing a system (seperate method since everything must be deserialized before calling)
        /// </summary>
        public void Rebuild(List<StarSystem> systems, List<Empire> empires, List<IProject> projects)
        {
            foreach (string neighbor in m_neighborNames)
            {
                m_neighbors.Add(systems.Find((system) => system.Name == neighbor));
            }

            m_owner = empires.Find((empire) => empire.Name == m_ownerName);

            if (m_currentProjectID != "")
            {
                m_currentProject = projects.Find((project) => project.ID == m_currentProjectID);
            }

            foreach ((string empireName, int shipIndex) in m_deserializedShips)
            {
                m_ships.Add((empires.Find((empire) => empire.Name == empireName), shipIndex));
            }
        }
        #endregion
    }
}