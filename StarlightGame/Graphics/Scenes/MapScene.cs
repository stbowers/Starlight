using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightGame.GameCore;
using StarlightGame.GameCore.Field;
using StarlightGame.GameCore.Field.Galaxy;
using StarlightGame.Graphics.Objects;

namespace StarlightGame.Graphics.Scenes
{
    public class MapScene : Scene
    {
        VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;
        EventManager m_eventManager;

        GameState m_gameState;

        // Objects
        Vulkan2DSprite[] m_starSprites;
        StarOutline m_starOutline;
        HyperlaneOverlay m_hyperlaneOverlay;

        VulkanUIButton m_nextTurnButton;
        VulkanTextObject m_statusText;

        // Canvases
        VulkanCanvas m_canvas;
        VulkanCanvas m_mapCanvas;
        VulkanCanvas m_statusCanvas;

        // Animation thread
        Thread m_animationThread;

        // Children scenes

        public MapScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager, GameState gameState) :
        base(new Camera(new FVec3(0.0f, 0.0f, 2.0f), FVec3.Zero, FVec3.Up), (float)System.Math.PI / 2, apiManager.GetSwapchainImageExtent().Width, apiManager.GetSwapchainImageExtent().Height, 0.1f, 100.0f)
        {
            /* Layers:
			 * 	1: background
			 *  2: UI
			 */
            m_apiManager = apiManager;
            m_sceneManager = sceneManager;
            m_eventManager = eventManager;

            m_gameState = gameState;

            m_canvas = new VulkanCanvas(new FVec2(-1, -1), new FVec2(2, 2), new FVec2(2, 2));

            // Create map canvas
            m_mapCanvas = new VulkanCanvas(new FVec2(-1.0f, -.9f), new FVec2(1.6f, 1.8f), new FVec2(2.0f, 2.0f), false);
            m_canvas.AddObject(m_mapCanvas);

            // create star sprites
            Console.WriteLine("Creating sprites...");
            Quadrant[] quadrants = m_gameState.Field.Quadrants;
            for (int i = 0; i < quadrants.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        StarSystem system = quadrants[i][j, k];
                        if (system != null)
                        {
                            Star newStar = new Star(m_apiManager, system, OnStarClicked, OnStarMouseOver, OnStarMouseExit);
                            m_mapCanvas.AddObject(newStar);
                        }
                    }
                }
            }
            Console.WriteLine("Done!");

            // Create outline
            m_starOutline = new StarOutline(m_apiManager, gameState);
            m_canvas.AddObject(m_starOutline);

            // Create hyperlane overlay
            m_hyperlaneOverlay = new HyperlaneOverlay(m_apiManager, gameState.Field.Stars);
            m_mapCanvas.AddObject(m_hyperlaneOverlay);

            // Map background
            VulkanTextureCreateInfo mapBackgroundTextureInfo = new VulkanTextureCreateInfo();
            mapBackgroundTextureInfo.APIManager = m_apiManager;
            mapBackgroundTextureInfo.EnableMipmap = false;
            mapBackgroundTextureInfo.MagFilter = VulkanCore.Filter.Linear;
            mapBackgroundTextureInfo.MinFilter = VulkanCore.Filter.Linear;
            mapBackgroundTextureInfo.FileName = "./assets/Nebula2.jpg";
            VulkanTexture mapBackgroundTexture = VulkanTextureCache.GetTexture(mapBackgroundTextureInfo.FileName, mapBackgroundTextureInfo);
            Vulkan2DSprite mapBackground = new Vulkan2DSprite(m_apiManager, mapBackgroundTexture, new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), 1.0f);
            m_canvas.AddObject(mapBackground);

            // Status bar
            m_statusCanvas = new VulkanCanvas(new FVec2(-1.0f, .9f), new FVec2(2.0f, .1f), new FVec2(2.0f, 2.0f), false);
            m_canvas.AddObject(m_statusCanvas);

            VulkanTextureCreateInfo statusBackgroundTextureInfo = new VulkanTextureCreateInfo();
            statusBackgroundTextureInfo.APIManager = m_apiManager;
            statusBackgroundTextureInfo.EnableMipmap = false;
            statusBackgroundTextureInfo.MagFilter = VulkanCore.Filter.Nearest;
            statusBackgroundTextureInfo.MinFilter = VulkanCore.Filter.Nearest;
            statusBackgroundTextureInfo.FileName = "./assets/StatusBackground.png";
            VulkanTexture statusBackgroundTexture = VulkanTextureCache.GetTexture(statusBackgroundTextureInfo.FileName, statusBackgroundTextureInfo);
            Vulkan2DSprite statusBackground = new Vulkan2DSprite(m_apiManager, statusBackgroundTexture, new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), 1.0f);
            m_statusCanvas.AddObject(statusBackground);

            m_nextTurnButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Next Turn", 16, new FVec2(.8f, -1.0f), new FVec2(.2f, 2.0f),
                onClickDelegate: () =>
                {
                    // Tell game state we're done with our turn
                    m_gameState.NextTurn();

                    // Change button text while waiting for turn to process
                    m_nextTurnButton.UpdateText(StaticFonts.Font_Arial, "Processing...", 14);
                    m_nextTurnButton.Enabled = false;
                }
            );
            m_statusCanvas.AddObject(m_nextTurnButton);

            string statusText = string.Format("[{0}] Turn: {1}", m_gameState.PlayerEmpire.Name, m_gameState.Turn);
            m_statusText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, statusText, 16, new FVec2(-.97f, -.5f), 1.5f);
            m_statusCanvas.AddObject(m_statusText);

            m_eventManager.Subscribe(GameEvent.NextTurnID, (object sender, IEvent e) =>
            {
                // update turn text
                m_statusText.UpdateText(StaticFonts.Font_Arial, string.Format("[{0}] Turn: {1}", m_gameState.PlayerEmpire.Name, m_gameState.Turn), 16);

                // reset button text
                m_nextTurnButton.UpdateText(StaticFonts.Font_Arial, "Next Turn", 16);
                m_nextTurnButton.Enabled = true;
            });

            AddObject(m_canvas);

            // Start animation on new thread
            m_animationThread = new Thread(AnimateScreen);
            m_animationThread.Name = "Map scene animation";
            m_animationThread.Start();
        }

        // Animation
        public void AnimateScreen()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void OnStarMouseOver(Star star)
        {
        }

        public void OnStarMouseExit(Star star)
        {
        }

        public void OnStarClicked(Star star)
        {
            m_starOutline.FocusSystem(star);
        }
    }
}
