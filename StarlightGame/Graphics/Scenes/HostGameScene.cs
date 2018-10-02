using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;

namespace StarlightGame.Graphics.Scenes
{
	public class HostGameScene: Scene
	{
		VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;

		// Objects
        VulkanUIButton m_backButton;

		// Animation thread
		Thread m_animationThread;

		public HostGameScene(VulkanAPIManager apiManager, SceneManager sceneManager)
		{
			/* Layers:
			 * 	1: background
			 *  2: UI
			 */
			m_apiManager = apiManager;
            m_sceneManager = sceneManager;

            // create back button
			m_backButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Back", 20, new FVec2(-.1f, 0.0f), new FVec2(.2f, .1f), onBackClicked);
            AddObject(2, m_backButton);
		}

        // Button callbacks
        public void onBackClicked(){
            if (m_sceneManager.PeekScene() == this){
                m_sceneManager.PopScene();
            }
        }
	}
}
