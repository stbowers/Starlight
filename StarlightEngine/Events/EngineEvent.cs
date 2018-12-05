using System;

namespace StarlightEngine.Events
{
    public class EngineEvent : IEvent
    {
        public static string CommandSentID
        {
            get
            {
                return "SE_COMMAND_SENT";
            }
        }

        public static string SetMouseNormal
        {
            get
            {
                return "SE_MOUSE_NORMAL";
            }
        }

        public static string SetMouseSelect
        {
            get
            {
                return "SE_MOUSE_SELECT";
            }
        }

        public static string SetMouseLoading
        {
            get
            {
                return "SE_MOUSE_LOADING";
            }
        }

        public string Data;
    }
}