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

        public string Data;
    }
}