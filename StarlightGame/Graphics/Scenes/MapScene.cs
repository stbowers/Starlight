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
        VulkanCanvas m_canvas;

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
                            Star newStar = new Star(m_apiManager, system, OnStarClicked, OnStarMouseOver, OnStarMouseExit);
                            m_canvas.AddObject(newStar);
                        }
                    }
                }
            }
            Console.WriteLine("Done!");

            // Create outline
            m_starOutline = new StarOutline(m_apiManager, gameState);
            m_canvas.AddObject(m_starOutline);

            VulkanCanvas testcanvas = new VulkanCanvas(new FVec2(0.0f, 0.0f), new FVec2(.5f, .5f), new FVec2(2.0f, 2.0f));
            VulkanTextureCreateInfo testtexture = new VulkanTextureCreateInfo();
            testtexture.AnisotropyEnable = false;
            testtexture.APIManager = m_apiManager;
            testtexture.EnableMipmap = false;
            testtexture.FileName = "./assets/bricks.jpg";
            testtexture.MagFilter = VulkanCore.Filter.Linear;
            testtexture.MinFilter = VulkanCore.Filter.Linear;
            VulkanTexture _testtexture = VulkanTextureCache.GetTexture("./assets/bricks.jpg", testtexture);
            Vulkan2DSprite testsprite = new Vulkan2DSprite(m_apiManager, _testtexture, new FVec2(-1.5f, -1.5f), new FVec2(2.0f, 2.0f));
            testcanvas.AddObject(testsprite);
            AddObject(testcanvas);

            Vulkan2DRect testrect = new Vulkan2DRect(m_apiManager, new FVec2(0.0f, 0.0f), new FVec2(.5f, .5f), new FVec4(1.0f, 0.0f, 1.0f, 1.0f));
            AddObject(testrect);

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
            Console.WriteLine("Mouse Over");
        }

        public void OnStarMouseExit(Star star)
        {
            Console.WriteLine("Mouse Exit");
        }

        public void OnStarClicked(Star star)
        {
            Console.WriteLine("Mouse Click");
            m_starOutline.FocusSystem(star.System);
        }
    }
}
