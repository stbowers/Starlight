using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics
{
	public enum MouseButton
	{
		Primary,
		Secondary,
		Middle
	}

	public struct WindowManagerCallbacks
	{
		public delegate void KeyboardEventDelegate(Key key, KeyAction action, KeyModifiers modifiers);
		public delegate void MouseEventDelegate(MouseButton button, MouseAction action, KeyModifiers modifiers, FVec2 mousePosition, float scrollMotion);
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

		/* Gets the current mouse position in the window (top left is -1, -1, +x is to the right, +y is down)
		 */
		FVec2 GetMousePosition();

		/* Gets the current state of the given mouse button
		 */
		bool IsMouseButtonPressed(MouseButton button);

        /* Generate new events from window
         */
        void PollEvents();

		void SetKeyboardEventDelegate(WindowManagerCallbacks.KeyboardEventDelegate keyboardEventDelegate);
		void SetMouseEventDelegate(WindowManagerCallbacks.MouseEventDelegate mouseEventDelegate);

		/* getters for width and height
		 */
		int Width { get; }
		int Height { get; }
	}
}
