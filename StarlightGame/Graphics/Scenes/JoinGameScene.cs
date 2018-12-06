using System;
using System.Threading;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightGame.GameCore;
using StarlightGame.Network;

namespace StarlightGame.Graphics.Scenes
{
    public class JoinGameScene : Scene
    {
        VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;
        EventManager m_eventManager;

        GameState m_gameState = null;

        // Objects
        VulkanCanvas m_canvas;
        VulkanUIButton m_joinGameButton;
        VulkanTextObject m_gameIDText;

        // Sub-scenes
        Scene m_mapScene = null;

        // Animation thread
        Thread m_animationThread;

        int m_gameID;

        public JoinGameScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager) :
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

            // Game ID
            m_gameIDText = new VulkanTextObject(m_apiManager, StaticFonts.Font_Arial, "Game ID: N/A", 20, new FVec2(-0.1f, 0.0f), 2.0f);
            m_canvas.AddObject(m_gameIDText);

            // start game button
            m_joinGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Join Game", 20, new FVec2(-.1f, 0.1f), new FVec2(.4f, .1f), onJoinGameClicked, center: false);
            m_canvas.AddObject(m_joinGameButton);

            // Add subscriber for join command
            m_eventManager.Subscribe(EngineEvent.CommandSentID, onCommandEntered);

            // Add canvas to scene
            AddObject(m_canvas);
        }

        public void SetGameID(int gameID)
        {
            m_gameID = gameID;
            m_gameIDText.UpdateText(StaticFonts.Font_Arial, string.Format("Game ID: {0}", gameID), 20);
        }

        // Button callbacks
        public void onJoinGameClicked()
        {
            // Disable button and change text
            m_joinGameButton.UpdateText(StaticFonts.Font_Arial, "Waiting for Game to Start...", 16);
            m_joinGameButton.Enabled = false;

            // Create client
            new Client("http://localhost:5001", m_gameID);

            // Join game
            Empire playerEmpire = new Empire("Romulan Star Empire", new FVec4(1.0f, .2f, 0.0f, 1.0f), new FVec4(1.0f, .4f, 0.0f, 1.0f));
            Client.StaticClient.JoinGame(playerEmpire);
            Console.WriteLine("Joining game...");

            m_mapScene = new MapScene(m_apiManager, m_sceneManager, m_eventManager, GameState.State);
            m_sceneManager.PushScene(m_mapScene);
        }

        public void onCommandEntered(object sender, IEvent e)
        {
            EngineEvent engineEvent = (EngineEvent)e;

            string[] parts = engineEvent.Data.Split(" ");

            if (parts[0] == "join")
            {
                int gameID = Int32.Parse(parts[1]);

                SetGameID(gameID);
            }
        }
    }
}
