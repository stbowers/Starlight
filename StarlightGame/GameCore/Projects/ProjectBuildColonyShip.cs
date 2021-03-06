using System;
using System.Linq;
using StarlightGame.GameCore.Ships;
using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.GameCore.Projects
{
    [Project("Build Colony Ship", 3)]
    public class ProjectBuildColonyShip : IProject
    {
        public string ID
        {
            get
            {
                return "StarlightProject.BuildColonyShip";
            }
        }

        /// <summary>
        /// The description while working on the project
        /// </summary>
        public string Description
        {
            get
            {
                return "Building colony ship";
            }
        }

        /// <summary>
        /// Can the given empire start this project in the given system?
        /// </summary>
        public bool CanStart(Empire empire, StarSystem starSystem)
        {
            bool canBuild = true;

            // empire must not have a colony ship already
            canBuild &= (
                from ship in empire.Ships
                where ship is ColonyShip
                select ship
            ).Count() == 0;

            // the empire must have a colony in the given system
            canBuild &= starSystem.Owner == empire;
            canBuild &= starSystem.Colonized;

            // the system must not already be building a colony ship
            canBuild &= starSystem.CurrentProject != this;

            return canBuild;
        }

        /// <summary>
        /// Start the project with the given empire in the given system
        /// </summary>
        public void Start(Empire empire, StarSystem starSystem)
        {
            starSystem.CurrentProject = this;
            starSystem.ProjectTurnsLeft = 3;
        }

        /// <summary>
        /// Apply effects of the given empire finishing this project in the given system
        /// </summary>
        public void FinishProject(Empire empire, StarSystem starSystem)
        {
            IShip newColonyShip = new ColonyShip(empire, "USS Enterprise");
            empire.Ships.Add(newColonyShip);
            starSystem.Ships.Add(newColonyShip);
        }
    }
}