using System;
using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.GameCore.Projects
{
    /// <summary>
    /// Represents a project a star system may work on
    /// </summary>
    public interface IProject
    {
        /// <summary>
        /// A unique id for the project
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The description while working on the project
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Can the given empire start this project in the given system?
        /// </summary>
        bool CanStart(Empire empire, StarSystem starSystem);

        /// <summary>
        /// Start the project with the given empire in the given system
        /// </summary>
        void Start(Empire empire, StarSystem starSystem);

        /// <summary>
        /// Apply effects of the given empire finishing this project in the given system
        /// </summary>
        void FinishProject(Empire empire, StarSystem starSystem);
    }
}