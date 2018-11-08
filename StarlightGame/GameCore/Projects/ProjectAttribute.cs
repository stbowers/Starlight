using System;

namespace StarlightGame.GameCore.Projects
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProjectAttribute : Attribute
    {
        string m_description;
        int m_turns;

        public ProjectAttribute(string description, int turns)
        {
            m_description = description;
            m_turns = turns;
        }

        public string ProjectDescription
        {
            get
            {
                return m_description;
            }
        }

        public int Turns
        {
            get
            {
                return m_turns;
            }
        }
    }
}