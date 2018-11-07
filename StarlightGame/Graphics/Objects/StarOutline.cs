using System;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan;

using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.Graphics.Objects
{
    public class StarOutline : VulkanCanvas
    {
        VulkanAPIManager m_apiManager;

        VulkanTextObject m_systemNameText;
        VulkanTextObject m_ownerNameText;

        public StarOutline(VulkanAPIManager apiManager) :
        base(new FVec2(.6f, -.8f), new FVec2(.4f, 1.5f), new FVec2(2.0f, 2.0f))
        {
            // [ SYSTEM NAME ]
            // [ RESOURCES   ]
            // [ OWNER       ]
            // ---------------
            // [ SHIPS ]

            //UIScale = FMat4.Scale(new FVec3(3.0f, 1.0f, 1.0f));

            m_apiManager = apiManager;

            Vulkan2DRect background = new Vulkan2DRect(m_apiManager, new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), new FVec4(1.0f, 0.0f, .8f, 1.0f));
            AddObject(background);

            m_systemNameText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, "SYSTEM", 40, new FVec2(-1.0f, -1.0f), .3f, true);
            AddObject(m_systemNameText);
        }

        public void FocusSystem(StarSystem system)
        {
            m_systemNameText.UpdateText(StaticFonts.Font_Arial, system.Name, 40);
        }
    }
}