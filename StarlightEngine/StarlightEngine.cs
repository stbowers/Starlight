using System;
using System.Runtime.InteropServices;

namespace StarlightEngine
{
    /// <summary>
    /// Constants for the engine
    /// </summary>
    public static class EngineConstants
    {
        // Defines levels for threads/lock hierarchy
        #region Lock Hierarchy
        /// <summary> locks at this level protect resources directly calling system APIs </summary>
        public const int THREADLEVEL_DIRECTAPI = 0;

        /// <summary> locks at this level protect resources wrapping direct API calls </summary>
        public const int THREADLEVEL_DIRECTAPIMANAGERS = THREADLEVEL_DIRECTAPI + 1;

        /// <summary> locks at this level protect resources directly calling system APIs </summary>
        public const int THREADLEVEL_INDIRECTAPIMANAGERS = THREADLEVEL_DIRECTAPIMANAGERS + 1;
        
        /// <summary> locks at this level protect resources used during draw calls </summary>
        public const int THREADLEVEL_DRAW = THREADLEVEL_INDIRECTAPIMANAGERS + 1;

        /// <summary> locks at this level are used to protect resources used for recording swapchain buffers </summary>
        public const int THREADLEVEL_SWAPCHAIN_RECORD = THREADLEVEL_DRAW + 1;

        /// <summary> special level meant for swapchain lock </summary>
        public const int THREADLEVEL_SWAPCHAIN = THREADLEVEL_SWAPCHAIN_RECORD + 1;
        
        /// <summary> locks at this level protect direct collections of managed api resources (Direct meaning closer to a direct manager, but still a collection manager, like VulkanDescriptorSet) </summary>
        public const int THREADLEVEL_DIRECTMANAGEDCOLLECTION = THREADLEVEL_SWAPCHAIN + 1;

        /// <summary> locks at this level protect indirect collections of managed api resources (i.e. VulkanManagedBuffer and the like) </summary>
        public const int THREADLEVEL_INDIRECTMANAGEDCOLLECTION = THREADLEVEL_DIRECTMANAGEDCOLLECTION + 1;

        /// <summary> special level meant to lock for the event manager </summary>
        public const int THREADLEVEL_EVENTMANAGER = 10;
        #endregion
    }
}
