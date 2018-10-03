using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanBoxCollider: IVulkanCollider
	{
		/* A point p is inside of the box if:
		 *   v = m_transform * p, such that:
		 *   0 <= v.X <= 1,
		 *   0 <= v.Y <= 1,
		 *   0 <= v.Z <= 1
		 */
		FMat4 m_transform;

		// 2D box colider
		public VulkanBoxCollider(FVec2 position, FVec2 size)
		{
			m_transform = new FMat4(1.0f);
			m_transform *= FMat4.Scale(new FVec3(1 / size.X(), 1 / size.Y(), 0));
			m_transform *= FMat4.Translate(new FVec3(-position.X(), -position.Y(), 0));
		}

		public void Update()
		{
		}

		public bool IsCollision(FVec3 point)
		{
			FVec4 v = m_transform * new FVec4(point.X(), point.Y(), point.Z(), 1);

			if (((0 <= v.X()) && (v.X() <= 1)) &&
				((0 <= v.Y()) && (v.Y() <= 1)) &&
				((0 <= v.Z()) && (v.Z() <= 1)))
			{
				return true;
			}
			return false;
		}
	}
}
