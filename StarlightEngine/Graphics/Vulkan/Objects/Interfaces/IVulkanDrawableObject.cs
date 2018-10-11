using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Math;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
	/* Interface for objects which can be drawn with Vulkan
	 * RenderPasses should be a list of all render passes during which this object makes draw calls,
	 * Pipelines should have the same size as RenderPasses, each entry of Pipelines should be the pipeline to be bound for the corrosponding entry in RenderPasses
	 * BindableComponents: For each renderpass the same index in BindableComponents should be a list of components to bind before calling the draw function
	 * Draw: called to draw the object, given the current commandbuffer, pipeline, renderpass, and bound sets draw the geometry for the renderpass at drawIndex
	 */
	public interface IVulkanDrawableObject: IVulkanObject
    {
		RenderPass[] RenderPasses { get; }
		VulkanPipeline[] Pipelines { get; }
        IVulkanBindableComponent[][] BindableComponents { get; }
        void Draw(CommandBuffer commandBuffer, int swapchainIndex);

		// controls if the object should be drawn or not
		bool Visible { get; set; }
    }

	/// <summary>
	/// Default functions for getting and setting visible components in a collection object.
	/// Should be overwritten if custom functionality is needed for a specific object.
	/// </summary>
	public static class ICollectionObjectVisibleExtension{
		public static bool IsVisible<T>(this T collection)
		where T : ICollectionObject
		{
			bool visible = false;
			foreach (IGraphicsObject obj in collection.Objects){
				if (obj is IVulkanDrawableObject){
					visible |= (obj as IVulkanDrawableObject).Visible;
				}
				if (obj is ICollectionObject){
					visible |= (obj as ICollectionObject).IsVisible();
				}
			}
			return visible;
		}

		public static void SetVisible<T>(this T collection, bool isVisible)
		where T : ICollectionObject
		{
			foreach (IGraphicsObject obj in collection.Objects){
				if (obj is IVulkanDrawableObject){
					(obj as IVulkanDrawableObject).Visible = isVisible;
				}
				if (obj is ICollectionObject){
					(obj as ICollectionObject).SetVisible(isVisible);
				}
			}
		}
	}
}
