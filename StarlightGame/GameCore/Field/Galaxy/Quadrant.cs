using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StarlightEngine.Math;
using StarlightGame.GameCore.Projects;

namespace StarlightGame.GameCore.Field.Galaxy
{
    [Serializable()]
    public class Quadrant : ISerializable
    {
        #region Private Members
        const float QUADRANTSIZE = 1.0f;
        const float SECTOR_SIZE = QUADRANTSIZE / 4.0f;

        // rectangular array of 4x4 stars in this quadrant
        StarSystem[,] m_sectors = new StarSystem[4, 4];
        int m_quadrantNumber;

        #endregion

        #region Constructor
        public Quadrant(Shape galaxyShape, int quadrantNumber)
        {
            Random random = RNG.GetRNG();
            m_quadrantNumber = quadrantNumber;

            float[,] genBias = galaxyShape.GetGenerationBiases(quadrantNumber);
            FVec2 quadrantOffset = null;
            switch (quadrantNumber)
            {
                case 0:
                    quadrantOffset = new FVec2(0.0f, -1.0f);
                    break;
                case 1:
                    quadrantOffset = new FVec2(-1.0f, -1.0f);
                    break;
                case 2:
                    quadrantOffset = new FVec2(-1.0f, 0.0f);
                    break;
                case 3:
                    quadrantOffset = new FVec2(0.0f, 0.0f);
                    break;
            }

            for (int i = 0; i < m_sectors.GetLength(0); i++)
            {
                for (int j = 0; j < m_sectors.GetLength(1); j++)
                {
                    bool spawnStar = random.NextDouble() < genBias[i, j];
                    if (spawnStar)
                    {
                        FVec2 sectorOffset = new FVec2(i * SECTOR_SIZE, j * SECTOR_SIZE) + quadrantOffset;
                        float x = ((float)random.NextDouble() * SECTOR_SIZE);
                        float y = ((float)random.NextDouble() * SECTOR_SIZE);
                        FVec2 starLocation = new FVec2(x, y) + sectorOffset;
                        m_sectors[i, j] = new StarSystem(SystemNames.GetSystemName(), starLocation);
                    }
                }
            }

        }
        #endregion

        #region Public Properties
        public StarSystem this[int x, int y]
        {
            get
            {
                return m_sectors[x, y];
            }
        }
        public StarSystem[] Stars
        {
            get
            {
                List<StarSystem> stars = new List<StarSystem>();
                foreach (StarSystem system in m_sectors)
                {
                    if (system != null)
                    {
                        stars.Add(system);
                    }
                }
                return stars.ToArray();
            }
        }
        #endregion

        #region Serialization
        // Serialization function
        public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("Stars", m_sectors);
        }

        // Deserialization constructor
        public Quadrant(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            m_sectors = (StarSystem[,])serializationInfo.GetValue("Stars", typeof(StarSystem[,]));
        }

        /// <summary>
        /// Calls the rebuild function for each star in the quadrant
        /// </summary>
        public void RebuildQuadrant(List<StarSystem> systems, List<Empire> empires, List<IProject> projects)
        {
            foreach (StarSystem system in m_sectors)
            {
                system?.Rebuild(systems, empires, projects);
            }
        }
        #endregion
    }
}