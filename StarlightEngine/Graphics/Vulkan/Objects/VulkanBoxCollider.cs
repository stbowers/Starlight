using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
    public class VulkanBoxCollider : IVulkanCollider
    {
        /* A point p is inside of the box if:
		 *   v = m_transform * p, such that:
		 *   0 <= v.X <= 1,
		 *   0 <= v.Y <= 1,
		 *   0 <= v.Z <= 1
		 */
		// m_transform: transforms object space into collision space
        FMat4 m_transform;

		// m_modelTransformInverse: transforms world space into object space
		FMat4 m_modelTransformInverse;

        // 2D box colider
        public VulkanBoxCollider(FVec2 position, FVec2 size)
        {
            m_transform = new FMat4(1.0f);
            m_transform *= FMat4.Scale(new FVec3(1 / size.X(), 1 / size.Y(), 1.0f));
            m_transform *= FMat4.Translate(new FVec3(-position.X(), -position.Y(), 0));
        }

        public void Update()
        {
        }

        public void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform)
        {
			m_modelTransformInverse = FMat4.Invert(modelTransform);
        }

        public bool IsPointInside(FVec3 point)
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

        public bool DoesIntersect(Ray ray)
        {
			// Transform ray into collision space (world space -> object space -> collision space)
			Ray transformedRay = m_transform * /*m_modelTransformInverse **/ ray;

			// Clip ray with each plane of the box collider
			transformedRay.ClipX(0, 1);
			transformedRay.ClipY(0, 1);
			transformedRay.ClipZ(0, 1);

			bool intersect = transformedRay.IsSegment();
			return intersect;
        }

        public (EventManager.HandleEventDelegate, EventType)[] EventListeners
        {
            get
            {
                return new(EventManager.HandleEventDelegate, EventType)[] { };
            }
        }
    }
}
