using System;
using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;
using StarlightGame.GameCore;
using StarlightGame.GameCore.Field;

namespace StarlightGame.Graphics.Scenes
{
	public class MapScene: Scene
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

		public MapScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager, GameState gameState)
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
            StarSystem[] stars = m_gameState.Field.Stars;
            m_starSprites = new Vulkan2DSprite[stars.Length];
            for (int i = 0; i < stars.Length; i++){
                m_starSprites[i] = new Vulkan2DSprite(m_apiManager, "./assets/Star.png", stars[i].Location, new FVec2(.03f, .03f));
                AddObject(2, m_starSprites[i]);
            }
            Console.WriteLine("Done!");

			// Start animation on new thread
			m_animationThread = new Thread(AnimateScreen);
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
