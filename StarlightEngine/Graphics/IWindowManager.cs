using System;
using System.Collections.Generic;
using glfw3;

namespace StarlightEngine.Graphics
{
	public enum KeyAction
	{
		Press,
		Release,
		Repeat
	}

	public struct WindowManagerCallbacks
	{
		public delegate void KeyboardEventDelegate(Key key, KeyAction action, List<KeyModifier> modifiers);
	}

	/* Interface that any window manager class should implement
	 */
	public interface IWindowManager
	{
		/* Returns a list of extensions a vulkan instance will need to load in order to present to this window's surface
		 */
		string[] GetVulkanExtensions();

		/* Returns the vulkan surface for the window
		 */
		VulkanCore.Khr.SurfaceKhr GetVulkanSurface(VulkanCore.Instance instance);

        /* Should the window close
         */
        bool ShouldWindowClose();

        /* Generate new events from window
         */
        void PollEvents();

		void SetKeyboardEventDelegate(WindowManagerCallbacks.KeyboardEventDelegate keyboardEventDelegate);

		/* getters for width and height
		 */
		int Width { get; }
		int Height { get; }
	}
}
