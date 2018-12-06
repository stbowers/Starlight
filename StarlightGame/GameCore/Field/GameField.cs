using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StarlightEngine.Math;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightGame.GameCore.Projects;

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
            // Q2 | Q1
            // -------
            // Q3 | Q4
            for (int i = 0; i < 4; i++)
            {
                m_quadrants[i] = new Quadrant(Shape.Spiral2, i);
            }

            // Generate connections between stars
            // loop over each star, giving it an x and y coordinate (galaxy is 8 * 8)
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    StarSystem system = GetSystem(x, y);

                    if (system == null)
                    {
                        continue;
                    }

                    // generate random connections
                    // generate between 1 and 3 connections
                    int numConnections = (RNG.GetRNG().Next() % 3) + 1;
                    for (int i = 0; i < numConnections; i++)
                    {
                        // generate a connection with this system and one of the systems surrounding it
                        // 0 1 2
                        // 3 x 4
                        // 5 6 7
                        int neighborIndex = RNG.GetRNG().Next() % 8; // 0-7
                        StarSystem neighbor;
                        switch (neighborIndex)
                        {
                            case 0:
                                neighbor = GetSystem(x - 1, y - 1);
                                break;
                            case 1:
                                neighbor = GetSystem(x, y - 1);
                                break;
                            case 2:
                                neighbor = GetSystem(x + 1, y - 1);
                                break;
                            case 3:
                                neighbor = GetSystem(x - 1, y);
                                break;
                            case 4:
                                neighbor = GetSystem(x + 1, y);
                                break;
                            case 5:
                                neighbor = GetSystem(x - 1, y + 1);
                                break;
                            case 6:
                                neighbor = GetSystem(x, y + 1);
                                break;
                            case 7:
                                neighbor = GetSystem(x + 1, y + 1);
                                break;
                            default:
                                neighbor = null;
                                break;
                        }

                        if (neighbor != null)
                        {
                            system.AddNeighbor(neighbor);
                        }
                    }
                }
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

        public StarSystem[] Stars
        {
            get
            {
                return
                (
                    from quadrant in m_quadrants
                    from star in quadrant.Stars
                    select star
                ).ToArray();
            }
        }
        #endregion

        #region Public methods
        public StarSystem GetSystem(int x, int y)
        {
            int quadIndex = 0;
            if (x > 3 && y < 3)
            {
                quadIndex = 0;
            }
            else if (x < 3 && y < 3)
            {
                quadIndex = 1;
            }
            else if (x < 3 && y > 3)
            {
                quadIndex = 2;
            }
            else
            {
                quadIndex = 3;
            }

            StarSystem system;
            try
            {
                system = m_quadrants[quadIndex][x % 4, y % 4];
            }
            catch
            {
                system = null;
            }
            return system;
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("Quadrants", m_quadrants);
        }

        // Deserialization constructor
        public GameField(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_rng = RNG.GetRNG();

            m_quadrants = (Quadrant[])serializationInfo.GetValue("Quadrants", typeof(Quadrant[]));
        }

        /// <summary>
        /// Rebuilds the stars in the field after deserialization
        /// </summary>
        public void RebuildField(List<Empire> empires, List<IProject> projects)
        {
            List<StarSystem> systems = new List<StarSystem>(Stars);
            foreach (Quadrant quadrant in m_quadrants)
            {
                quadrant.RebuildQuadrant(systems, empires, projects);
            }
        }
        #endregion
    }
}