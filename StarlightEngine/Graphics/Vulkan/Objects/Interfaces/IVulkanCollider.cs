using System;
using StarlightEngine.Graphics.Math;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
	public interface IVulkanCollider: IVulkanObject
	{
		bool IsCollision(FVec3 point);
	}
}
