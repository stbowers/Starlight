using System;
using System.Diagnostics;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

using StarlightGame.GameCore.Field.Galaxy;

namespace StarlightGame.Graphics.Objects
{
    public class StarOutline : VulkanCanvas
    {
        VulkanAPIManager m_apiManager;
        Stopwatch m_timer;

        VulkanTextObject m_systemNameText;
        VulkanTextObject m_systemStatusText;
        VulkanTextObject m_currentProjectText;

        VulkanUIButton m_claimSystemButton;
        VulkanUIButton m_colonizeSystemButton;

        GameCore.GameState m_gameState;
        StarSystem m_currentSystem;

        public StarOutline(VulkanAPIManager apiManager, GameCore.GameState gameState) :
        base(new FVec2(.6f, -.8f), new FVec2(.4f, 1.5f), new FVec2(2.0f, 2.0f))
        {
            // [ SYSTEM NAME ]
            // [ RESOURCES   ]
            // [ OWNER       ]
            // ---------------
            // [ SHIPS ]
            // [ CLAIM SYSTEM ]

            m_apiManager = apiManager;
            m_gameState = gameState;
            m_timer = new Stopwatch();
            m_timer.Start();

            VulkanTextureCreateInfo backgroundImageInfo = new VulkanTextureCreateInfo();
            backgroundImageInfo.APIManager = apiManager;
            backgroundImageInfo.FileName = "./assets/OutlineBackground.png";
            backgroundImageInfo.EnableMipmap = false;
            backgroundImageInfo.MagFilter = VulkanCore.Filter.Nearest;
            backgroundImageInfo.MinFilter = VulkanCore.Filter.Nearest;
            backgroundImageInfo.AnisotropyEnable = false;
            VulkanTexture backgroundImageTexture = VulkanTextureCache.GetTexture(backgroundImageInfo.FileName, backgroundImageInfo);
            Vulkan2DSprite background = new Vulkan2DSprite(apiManager, backgroundImageTexture, new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f));
            AddObject(background);

            m_systemNameText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, "N/A", 20, new FVec2(-.93f, -.97f), .3f, true);
            AddObject(m_systemNameText);

            m_systemStatusText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, "N/A", 20, new FVec2(-.93f, -.88f), .3f, true);
            AddObject(m_systemStatusText);

            m_currentProjectText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, "N/A", 20, new FVec2(-.93f, -.79f), .3f, true);
            AddObject(m_currentProjectText);

            m_claimSystemButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Claim System", 20, new FVec2(-.93f, .91f), new FVec2(1.86f, .09f), center: false, onClickDelegate: ClaimSystemButtonClicked);
            m_claimSystemButton.SetVisible(false);
            AddObject(m_claimSystemButton);
            m_colonizeSystemButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Colonize System", 20, new FVec2(-.93f, .91f), new FVec2(1.86f, .09f), center: false, onClickDelegate: ColonizeSystemButtonClicked);
            m_colonizeSystemButton.SetVisible(false);
            AddObject(m_colonizeSystemButton);
        }

        public void FocusSystem(StarSystem system)
        {
            m_currentSystem = system;
            m_systemNameText.UpdateText(StaticFonts.Font_Arial, system.Name, 20);

            if (system.Owner == null)
            {
                m_systemStatusText.UpdateText(StaticFonts.Font_Arial, "Unclaimed", 20);
                m_claimSystemButton.SetVisible(true);
                m_colonizeSystemButton.SetVisible(false);
            }
            else
            {
                string status = "";
                if (m_currentSystem.Colonized)
                {
                    status += "Colonized by: ";
                }
                else
                {
                    status += "Claimed by: ";
                }
                status += system.Owner.Name;
                m_systemStatusText.UpdateText(StaticFonts.Font_Arial, status, 8);
                m_claimSystemButton.SetVisible(false);
                if (system.Owner == m_gameState.PlayerEmpire)
                {
                    if (system.Colonized)
                    {
                        m_colonizeSystemButton.SetVisible(false);
                    }
                    else
                    {
                        m_colonizeSystemButton.SetVisible(true);
                    }
                }
                else
                {
                    m_colonizeSystemButton.SetVisible(false);
                }
            }
        }

        double lastClick = 0.0f;
        public void ClaimSystemButtonClicked()
        {
            if (m_timer.Elapsed.TotalSeconds - lastClick > .01)
            {
                lastClick = m_timer.Elapsed.TotalSeconds;
                m_currentSystem.Owner = m_gameState.PlayerEmpire;
                FocusSystem(m_currentSystem);
            }
        }

        public void ColonizeSystemButtonClicked()
        {
            if (m_timer.Elapsed.TotalSeconds - lastClick > .01)
            {
                lastClick = m_timer.Elapsed.TotalSeconds;
                m_currentSystem.Colonized = true;
                FocusSystem(m_currentSystem);
            }
        }
    }
}