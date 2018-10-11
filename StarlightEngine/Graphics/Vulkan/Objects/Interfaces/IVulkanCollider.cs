using System;
using StarlightEngine.Graphics.Math;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
	public interface IVulkanCollider : IVulkanObject
	{
		/// <summary>
		/// Tests if a 3d point is inside the collider
		/// </summary>
		bool IsPointInside(FVec3 point);

		/// <summary>
		/// Tests if the given ray intersects the collider
		/// </summary>
		bool DoesIntersect(Ray ray);
	}
}
