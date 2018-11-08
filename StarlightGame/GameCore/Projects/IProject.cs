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
        /// Get the description for starting the project
        /// </summary>
        string GetStartDescription();

        /// <summary>
        /// Get the description while working on the project
        /// </summary>
        string GetBuildDescription();

        /// <summary>
        /// Can the given empire start this project in the given system?
        /// </summary>
        bool CanStart(Empire empire, StarSystem starSystem);

        /// <summary>
        /// The number of turns required to finish this project
        /// </summary>
        int Turns { get; }

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