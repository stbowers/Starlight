using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightGame.Graphics.Scenes
{
	public class TitleScene: Scene
	{
		VulkanAPIManager m_apiManager;
		SceneManager m_sceneManager;
		EventManager m_eventManager;

		// Objects
		Vulkan2DSprite m_title;
		Vulkan2DProgressBar m_loadingBar;
		VulkanUIButton m_hostGameButton;

		// Animation thread
		Thread m_animationThread;

		// Children scenes
		Scene m_hostGameScene;

		public TitleScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager)
		{
			/* Layers:
			 * 	1: background
			 *  2: UI
			 */
			m_apiManager = apiManager;
			m_sceneManager = sceneManager;
			m_eventManager = eventManager;

			m_title = new Vulkan2DSprite(apiManager, "./assets/Title.png", new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f));
			AddObject(1, m_title);

			float fill = 0.0f;
			m_loadingBar = new Vulkan2DProgressBar(apiManager, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
			AddObject(2, m_loadingBar);

			// Host Game
			m_hostGameScene = new HostGameScene(m_apiManager, m_sceneManager, m_eventManager);
			m_hostGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Host Game", 20, new FVec2(-.1f, 0.0f), new FVec2(.2f, .1f), onHostGameClicked);
			AddObject(2, m_hostGameButton);
			m_hostGameButton.SetVisible(false);

			// Start animation on new thread
			m_animationThread = new Thread(AnimateTitleScreen);
			m_animationThread.Start();
		}

		// Animation
		public void AnimateTitleScreen()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// animate loading bar (2s)
			while (stopwatch.ElapsedMilliseconds / 1000.0f < 2.0f)
			{
				m_loadingBar.UpdatePercentage(stopwatch.ElapsedMilliseconds / 2000.0f);
				Thread.Sleep(10);
			}

			// set host game button to visible
			m_hostGameButton.SetVisible(true);
		}

		// Button delegates
		public void onHostGameClicked(){
			m_sceneManager.PushScene(m_hostGameScene);
		}
	}
}
