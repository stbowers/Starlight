using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Objects;
using System.Collections.Generic;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanUIButton : ICollectionObject
	{
		VulkanAPIManager m_apiManager;

		// objects
		VulkanTextObject m_text;
		Vulkan2DRect m_mouseOverHighlight;
		Vulkan2DRect m_mouseClickHighlight;
		IVulkanObject[] m_objects;

		// collider
		VulkanBoxCollider m_collider;

		// delegates
		public delegate void OnClickDelegate();
		OnClickDelegate m_onClickDelegate;

		public VulkanUIButton(VulkanAPIManager apiManager, AngelcodeFont font, string text, int fontSize, FVec2 location, FVec2 size, OnClickDelegate onClickDelegate)
		{
			m_apiManager = apiManager;
			m_onClickDelegate = onClickDelegate;

			// center text
			float textWidth = AngelcodeFontLoader.GetWidthOfString(font, fontSize, text) / 640.0f;
			float textHeight = AngelcodeFontLoader.GetHeightOfString(font, fontSize, text, size.X()) / 360.0f;
			FVec2 textOffset = new FVec2(location.X() + ((size.X() - textWidth) / 2.0f), location.Y() + ((size.Y() - textHeight) / 2.0f));

			m_text = new VulkanTextObject(apiManager, font, text, fontSize, textOffset, size.X());
			m_mouseOverHighlight = new Vulkan2DRect(apiManager, location, size, new FVec4(1.0f, 1.0f, 1.0f, .2f));
			m_mouseOverHighlight.Visible = false;
			m_mouseClickHighlight = new Vulkan2DRect(apiManager, location, size, new FVec4(.8f, .8f, .8f, .2f));
			m_mouseClickHighlight.Visible = false;
			m_objects = new IVulkanObject[] { m_text, m_mouseOverHighlight, m_mouseClickHighlight };

			m_collider = new VulkanBoxCollider(location, size);
		}

		public void Update()
		{
			FVec2 mouseVector = m_apiManager.GetWindowManager().GetMousePosition();
			bool isClicked = m_apiManager.GetWindowManager().IsMouseButtonPressed(MouseButton.Left);
			if (m_collider.IsCollision(new FVec3(mouseVector.X(), mouseVector.Y(), 0.0f)))
			{
				if (isClicked)
				{
					m_mouseClickHighlight.Visible = true;
					m_mouseOverHighlight.Visible = false;

					m_onClickDelegate();
				}
				else
				{
					m_mouseClickHighlight.Visible = false;
					m_mouseOverHighlight.Visible = true;
				}
			}
			else
			{
				m_mouseClickHighlight.Visible = false;
				m_mouseOverHighlight.Visible = false;
			}
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
