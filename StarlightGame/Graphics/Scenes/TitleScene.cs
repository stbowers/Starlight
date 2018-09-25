using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;

namespace StarlightGame.Graphics.Scenes
{
	public class TitleScene: Scene
	{
		VulkanAPIManager m_apiManager;

		// Objects
		Vulkan2DSprite m_title;
		Vulkan2DProgressBar m_loadingBar;

		// Animation thread
		Thread m_animationThread;

		public TitleScene(VulkanAPIManager apiManager)
		{
			/* Layers:
			 * 	1: background
			 *  2: UI
			 */
			m_apiManager = apiManager;

			m_title = new Vulkan2DSprite(apiManager, "./assets/Title.png", new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f));
			AddObject(1, m_title);

			float fill = 0.0f;
			m_loadingBar = new Vulkan2DProgressBar(apiManager, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
			AddObject(2, m_loadingBar);

			// new game
			/*
			Scene newGameScene = new Scene();
			newGameScene.AddObject(1, textTest);
			newGameScene.AddObject(1, mousePosText);
			StartGameButtonWrapper button = new StartGameButtonWrapper(apiManager, titleScene, arialFont, sceneManager, newGameScene);
			*/

			// Start animation on new thread
			//m_animationThread = new Thread(AnimateTitleScreen);
			//m_animationThread.Start();
		}

		// Animation
		public void AnimateTitleScreen()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// animate loading bar (5s)
			while (stopwatch.ElapsedMilliseconds / 1000.0f < 5.0f)
			{
				m_loadingBar.UpdatePercentage(stopwatch.ElapsedMilliseconds / 5000.0f);
				Thread.Sleep(10);
			}
		}
	}
}
