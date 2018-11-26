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
    public class HostGameScene : Scene
    {
        VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;
        EventManager m_eventManager;

        GameState m_gameState = null;

        // Objects
        VulkanCanvas m_canvas;
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

            m_canvas = new VulkanCanvas(new FVec2(-1, -1), new FVec2(2, 2), new FVec2(2, 2));

            // start game button
            m_startGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Start Game", 20, new FVec2(0.0f, 0.0f), new FVec2(.2f, .1f), onStartGameClicked);
            m_canvas.AddObject(m_startGameButton);

            // Add canvas to scene
            AddObject(m_canvas);
        }

        // Button callbacks
        public void onStartGameClicked()
        {
            // make new gamestate
            if (m_gameState == null)
            {
                m_gameState = new GameState(
                    "United Federation of Planets",
                    new FVec4(41.0f / 255.0f, 217.0f / 255.0f, 244.0f / 255.0f, 1.0f),
                    new FVec4(41.0f / 255.0f, 129.0f / 255.0f, 244.0f / 255.0f, 1.0f)
                );
                m_mapScene = new MapScene(m_apiManager, m_sceneManager, m_eventManager, m_gameState);
            }

            m_sceneManager.PushScene(m_mapScene);
        }
    }
}
