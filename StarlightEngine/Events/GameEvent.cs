using System;

namespace StarlightEngine.Events
{
    public class GameEvent : IEvent
    {
        public static string NextTurnID
        {
            get
            {
                return "SG_NEXT_TURN";
            }
        }
    }
}