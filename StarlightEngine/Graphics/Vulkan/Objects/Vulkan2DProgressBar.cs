using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class Vulkan2DProgressBar: ICollectionObject
	{
		VulkanAPIManager m_apiManager;

		FVec2 m_size;

		// Objects
		Vulkan2DRect m_fill;
		Vulkan2DRectOutline m_outline;
		IVulkanObject[] m_objects;

		public Vulkan2DProgressBar(VulkanAPIManager apiManager, FVec2 position, FVec2 size, float percentFilled, FVec4 color)
		{
			m_apiManager = apiManager;
			m_size = size;

			float fill = Functions.Clamp(percentFilled, 0.0f, 1.0f);
			FVec2 fillSize = new FVec2(fill * size.X, size.Y);
			m_fill = new Vulkan2DRect(apiManager, position, fillSize, color);
			m_outline = new Vulkan2DRectOutline(apiManager, position, size, color);

			m_objects = new IVulkanObject[] { m_fill, m_outline };
		}

		public void Update()
		{
		}

		public void UpdatePercentage(float newPercentage)
		{
			float fill = Functions.Clamp(newPercentage, 0.0f, 1.0f);
			FVec2 fillSize = new FVec2(fill * m_size.X, m_size.Y);

			m_fill.UpdateSize(fillSize);
		}

		public IGraphicsObject[] Objects
		{
			get
			{
				return m_objects;
			}
		}
	}
}
