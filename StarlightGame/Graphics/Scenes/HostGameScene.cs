using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightGame.GameCore;

namespace StarlightGame.Graphics.Scenes
{
	public class HostGameScene: Scene
	{
		VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;
		EventManager m_eventManager;

		GameState m_gameState = null;

		// Objects
        VulkanUIButton m_startGameButton;
        VulkanUIButton m_backButton;

		// Sub-scenes
		Scene m_mapScene = null;

		// Animation thread
		Thread m_animationThread;

		public HostGameScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager) :
		base(new Camera(new FVec3(0.0f, 0.0f, 2.0f), FVec3.Zero, FVec3.Up), (float)System.Math.PI / 2, apiManager.GetSwapchainImageExtent().Width, apiManager.GetSwapchainImageExtent().Height, 0.1f, 100.0f)
		{
			/* Layers:
			 * 	1: background
			 *  2: UI
			 */
			m_apiManager = apiManager;
            m_sceneManager = sceneManager;
			m_eventManager = eventManager;

			// start game button
			m_startGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Start Game", 20, new FVec2(0.0f, 0.0f), new FVec2(.2f, .1f), onStartGameClicked);
			AddObject(2, m_startGameButton);

            // create back button
			m_backButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Back", 20, new FVec2(-.5f, .5f), new FVec2(.2f, .1f), onBackClicked);
            AddObject(2, m_backButton);
		}

        // Button callbacks
		public void onStartGameClicked(){
			// make new gamestate
			if (m_gameState == null){
				m_gameState = new GameState();
				m_mapScene = new MapScene(m_apiManager, m_sceneManager, m_eventManager, m_gameState);
			}

			m_sceneManager.PushScene(m_mapScene);
		}

        public void onBackClicked(){
			m_sceneManager.PopScene();
        }
	}
}
