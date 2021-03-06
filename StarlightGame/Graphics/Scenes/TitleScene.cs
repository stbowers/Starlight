﻿using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

using StarlightGame.GameCore;
using StarlightGame.Network;

namespace StarlightGame.Graphics.Scenes
{
    public class TitleScene : Scene
    {
        VulkanAPIManager m_apiManager;
        SceneManager m_sceneManager;
        EventManager m_eventManager;

        // Objects
        VulkanCanvas m_canvas;
        Vulkan2DSprite m_background;
        Vulkan2DSprite m_title;
        Vulkan2DProgressBar m_loadingBar;

        VulkanUIButton m_hostGameButton;
        VulkanUIButton m_joinGameButton;
        VulkanUIButton m_optionsButton;
        VulkanUIButton m_exitGameButton;

        //Vulkan3DLine m_xAxis;
        //Vulkan3DLine m_yAxis;
        //Vulkan3DLine m_zAxis;
        //Vulkan3DLine m_mouseRay;

        // Animation thread
        Thread m_animationThread;

        // Children scenes
        Scene m_hostGameScene;
        Scene m_joinGameScene;
        Scene m_mapScene;

        public TitleScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager) :
        base(new Camera(new FVec3(0.0f, 0.0f, 1.5f), FVec3.Zero, FVec3.Up), (float)System.Math.PI / 2, apiManager.GetSwapchainImageExtent().Width, apiManager.GetSwapchainImageExtent().Height, 0.1f, 100.0f)
        {
            /* Layers:
			 * 	1: background
			 *  2: UI
			 */
            m_apiManager = apiManager;
            m_sceneManager = sceneManager;
            m_eventManager = eventManager;

            m_canvas = new VulkanCanvas(new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), new FVec2(2.0f, 2.0f));
            m_canvas.Visible = true;
            //FMat4 model = FMat4.Translate(new FVec3(0.0f, 0.0f, 0.0f)) * FMat4.Identity;
            //m_canvas = new VulkanCanvas(model, new FVec2(2.0f, 2.0f), Projection, Camera.View);
            //m_canvas = new VulkanCanvas(FVec3.Zero, Quaternion.Identity, Projection, Camera.View);

            FVec3 origin = new FVec3(0, 0, 0);
            //m_xAxis = new Vulkan3DLine(apiManager, origin, new FVec3(100, 0, 0), new FVec4(1.0f, 0.0f, 0.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            //m_yAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 100, 0), new FVec4(0.0f, 1.0f, 0.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            //m_zAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 0, 100), new FVec4(0.0f, 0.0f, 1.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            //m_mouseRay = new Vulkan3DLine(apiManager, origin, new FVec3(1, -10, 10), new FVec4(1.0f, 1.0f, .2f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            //m_eventManager.Subscribe(MouseEvent.ID, MouseRayEventHander);
            //AddObject(m_xAxis);
            //AddObject(m_yAxis);
            //AddObject(m_zAxis);
            //AddObject(m_mouseRay);

            //m_background = new Vulkan2DSprite(apiManager, "./assets/bricks.jpg", new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f));
            //AddObject(0, m_background);
            //m_canvas.AddObject(m_background);

            VulkanTextureCreateInfo createInfo = new VulkanTextureCreateInfo();
            createInfo.APIManager = m_apiManager;
            createInfo.FileName = "./assets/Title3.png";
            createInfo.EnableMipmap = false;
            createInfo.MagFilter = VulkanCore.Filter.Nearest;
            createInfo.MinFilter = VulkanCore.Filter.Nearest;
            createInfo.AnisotropyEnable = true;
            createInfo.MaxAnisotropy = 16;
            VulkanTexture titleTexture = VulkanTextureCache.GetTexture("./assets/Title3.png", createInfo);
            m_title = new Vulkan2DSprite(apiManager, titleTexture, new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f), 0.0f);
            //AddObject(1, m_title);
            m_canvas.AddObject(m_title);

            float fill = 0.0f;
            m_loadingBar = new Vulkan2DProgressBar(apiManager, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
            //AddObject(2, m_loadingBar);
            m_canvas.AddObject(m_loadingBar);

            // Host Game
            m_hostGameScene = new HostGameScene(m_apiManager, m_sceneManager, m_eventManager);
            m_hostGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Host Game", 20, new FVec2(-.1f, -.3f), new FVec2(.2f, .1f), onHostGameClicked);
            m_hostGameButton.Visible = false;
            m_canvas.AddObject(m_hostGameButton);

            // Join Game
            m_joinGameScene = new JoinGameScene(m_apiManager, m_sceneManager, m_eventManager);
            m_joinGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Join Game", 20, new FVec2(-.1f, -.2f), new FVec2(.2f, .1f), joinGameButtonClicked);
            m_joinGameButton.Visible = false;
            m_canvas.AddObject(m_joinGameButton);

            // Options
            m_optionsButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Options", 20, new FVec2(-.1f, -.1f), new FVec2(.2f, .1f), optionsButtonClicked);
            m_optionsButton.Visible = false;
            m_canvas.AddObject(m_optionsButton);

            // Exit Game
            m_exitGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Exit Game", 20, new FVec2(-.1f, 0.0f), new FVec2(.2f, .1f), exitGameButtonClicked);
            m_exitGameButton.Visible = false;
            m_canvas.AddObject(m_exitGameButton);

            // background
            VulkanTextureCreateInfo backgroundTextureInfo = new VulkanTextureCreateInfo();
            backgroundTextureInfo.APIManager = m_apiManager;
            backgroundTextureInfo.EnableMipmap = false;
            backgroundTextureInfo.MagFilter = VulkanCore.Filter.Linear;
            backgroundTextureInfo.MinFilter = VulkanCore.Filter.Linear;
            backgroundTextureInfo.FileName = "./assets/Nebula.jpg";
            VulkanTexture backgroundTexture = VulkanTextureCache.GetTexture(backgroundTextureInfo.FileName, backgroundTextureInfo);
            Vulkan2DSprite background = new Vulkan2DSprite(m_apiManager, backgroundTexture, new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), 1.0f);
            m_canvas.AddObject(background);

            AddObject(m_canvas);

            // Start animation on new thread
            m_animationThread = new Thread(AnimateTitleScreen);
            m_animationThread.Name = "Title scene animation";
            m_animationThread.Start();
        }

        // Animation
        public void AnimateTitleScreen()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // animate loading bar to half way
            while (stopwatch.ElapsedMilliseconds / 1000.0f < 1.0f)
            {
                m_loadingBar.UpdatePercentage(stopwatch.ElapsedMilliseconds / 1500.0f);
                Thread.Sleep(1);
            }
            // animate loading bar rest of way
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds / 1000.0f < .25f)
            {
                m_loadingBar.UpdatePercentage(.75f + stopwatch.ElapsedMilliseconds / 1000.0f);
                Thread.Sleep(1);
            }
            m_loadingBar.UpdatePercentage(1.0f);
            m_loadingBar.Visible = false;

            // Move title sprite
            stopwatch.Restart();
            FMat4 proj = new FMat4(1.0f);
            FMat4 view = new FMat4(1.0f);
            while (stopwatch.ElapsedMilliseconds / 1000.0f < .5f)
            {
                FMat4 shift = FMat4.Translate(new FVec3(0.0f, -(stopwatch.ElapsedMilliseconds / 1250.0f), 0.0f));
                m_title.UpdateMVPData(m_canvas.Projection, m_canvas.View, m_canvas.Model * shift);
                Thread.Sleep(1);
            }


            // set host game button to visible
            m_hostGameButton.Visible = true;
            m_joinGameButton.Visible = true;
            m_optionsButton.Visible = true;
            m_exitGameButton.Visible = true;
        }

        // Button delegates
        public void onHostGameClicked()
        {
            // Create game state
            GameState.State = new GameState(
                "United Federation of Planets",
                new FVec4(0.1f, 0.8f, 1.0f, 1.0f),
                new FVec4(0.0f, 0.0f, 1.0f, 1.0f)
                );

            // set up server client
            new Client("http://localhost:5001", GameState.State);

            // update host game scene
            ((HostGameScene)m_hostGameScene).SetGameID(Client.StaticClient.GameID);

            m_sceneManager.PushScene(m_hostGameScene);
        }

        public void joinGameButtonClicked()
        {
            m_sceneManager.PushScene(m_joinGameScene);
        }

        public void optionsButtonClicked()
        {
        }

        public void exitGameButtonClicked()
        {
            m_apiManager.GetWindowManager().CloseWindow();
        }

        //bool m_rightButtonDown = false;
        //bool m_leftButtonDown = false;
        //FVec3 m_cameraPosition = new FVec3(-5.0f, -.5f, 3.0f);
        //public void MouseRayEventHander(object sender, IEvent e)
        //{
        //    // cast e to MouseEvent
        //    MouseEvent mouseEvent = e as MouseEvent;

        //    // Get ray from camera to mouse position on screen
        //    FVec3 start = FMat4.UnProject(Projection, Camera.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
        //    FVec3 end = FMat4.UnProject(Projection, Camera.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));

        //    //start = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(start.X(), start.Y(), start.Z(), 1.0f)).XYZ();
        //    //end = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(end.X(), end.Y(), end.Z(), 1.0f)).XYZ();

        //    m_mouseRay.UpdateLine(start, new FVec3(0, 0, 0));
        //    //m_mouseRay.UpdateLine(new FVec3(0, 0, 0), end);
        //}
    }
}
