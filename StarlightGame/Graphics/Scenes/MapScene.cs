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

            // Create sprite for each star
            Console.WriteLine("Creating sprites...");
            Quadrant[] quadrants = m_gameState.Field.Quadrants;
            List<Vulkan2DSprite> starSprites = new List<Vulkan2DSprite>();
            VulkanTextureCreateInfo createInfo = new VulkanTextureCreateInfo();
            createInfo.APIManager = m_apiManager;
            createInfo.EnableMipmap = false;
            createInfo.MagFilter = VulkanCore.Filter.Nearest;
            createInfo.MinFilter = VulkanCore.Filter.Nearest;
            for (int i = 0; i < quadrants.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        StarSystem system = quadrants[i][j, k];
                        if (system != null)
                        {
                            createInfo.FileName = "./assets/Star.png";
                            VulkanTexture starTexture = VulkanTextureCache.GetTexture("./assets/Star.png", createInfo);
                            Vulkan2DSprite starSprite = new Vulkan2DSprite(m_apiManager, starTexture, system.Location, new FVec2(.03f, .03f));
                            starSprites.Add(starSprite);
                            AddObject(2, starSprite);
                        }
                    }
                }
            }
            Console.WriteLine("Done!");

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
    }
}
