using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

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

        Vulkan3DLine m_xAxis;
        Vulkan3DLine m_yAxis;
        Vulkan3DLine m_zAxis;
        Vulkan3DLine m_mouseRay;

        // Animation thread
        Thread m_animationThread;

        // Children scenes
        Scene m_hostGameScene;

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
            //FMat4 model = FMat4.Translate(new FVec3(0.0f, 0.0f, 0.0f)) * FMat4.Identity;
            //m_canvas = new VulkanCanvas(model, new FVec2(2.0f, 2.0f), Projection, Camera.View);
            //m_canvas = new VulkanCanvas(FVec3.Zero, Quaternion.Identity, Projection, Camera.View);

            FVec3 origin = new FVec3(0, 0, 0);
            m_xAxis = new Vulkan3DLine(apiManager, origin, new FVec3(100, 0, 0), new FVec4(1.0f, 0.0f, 0.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            m_yAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 100, 0), new FVec4(0.0f, 1.0f, 0.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            m_zAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 0, 100), new FVec4(0.0f, 0.0f, 1.0f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            m_mouseRay = new Vulkan3DLine(apiManager, origin, new FVec3(1, -10, 10), new FVec4(1.0f, 1.0f, .2f, 1.0f), new FMat4(1.0f), Camera.View, Projection);
            m_eventManager.AddListener(MouseRayEventHander, EventType.Mouse);
            AddObject(0, m_xAxis);
            AddObject(0, m_yAxis);
            AddObject(0, m_zAxis);
            AddObject(0, m_mouseRay);

            //m_background = new Vulkan2DSprite(apiManager, "./assets/bricks.jpg", new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f));
            //AddObject(0, m_background);
            //m_canvas.AddObject(m_background);

            VulkanTextureCreateInfo createInfo = new VulkanTextureCreateInfo();
            createInfo.APIManager = m_apiManager;
            createInfo.FileName = "./assets/Title.png";
            createInfo.EnableMipmap = false;
            createInfo.MagFilter = VulkanCore.Filter.Nearest;
            createInfo.MinFilter = VulkanCore.Filter.Nearest;
            VulkanTexture titleTexture = VulkanTextureCache.GetTexture("./assets/Title.png", createInfo);
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
            m_hostGameButton.SetVisible(false);
            m_canvas.AddObject(m_hostGameButton);

            m_joinGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Join Game", 20, new FVec2(-.1f, -.2f), new FVec2(.2f, .1f), joinGameButtonClicked);
            m_joinGameButton.SetVisible(false);
            m_canvas.AddObject(m_joinGameButton);
            m_optionsButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Options", 20, new FVec2(-.1f, -.1f), new FVec2(.2f, .1f), optionsButtonClicked);
            m_optionsButton.SetVisible(false);
            m_canvas.AddObject(m_optionsButton);
            m_exitGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Exit Game", 20, new FVec2(-.1f, 0.0f), new FVec2(.2f, .1f), exitGameButtonClicked);
            m_exitGameButton.SetVisible(false);
            m_canvas.AddObject(m_exitGameButton);

            AddObject(1, m_canvas);

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

            // animate loading bar
            while (stopwatch.ElapsedMilliseconds / 1000.0f < 1.0f)
            {
                m_loadingBar.UpdatePercentage(stopwatch.ElapsedMilliseconds / 1000.0f);
                Thread.Sleep(1);
            }
            m_loadingBar.UpdatePercentage(1.0f);
            m_loadingBar.SetVisible(false);

            // Move title sprite
            stopwatch.Restart();
            FMat4 proj = new FMat4(1.0f);
            FMat4 view = new FMat4(1.0f);
            while (stopwatch.ElapsedMilliseconds / 1000.0f < .75f)
            {
                m_loadingBar.UpdatePercentage(stopwatch.ElapsedMilliseconds / 750.0f);
                FMat4 shift = FMat4.Translate(new FVec3(0.0f, -(stopwatch.ElapsedMilliseconds / 1750.0f), 0.0f));
                m_title.UpdateMVPData(m_canvas.Projection, m_canvas.View, m_canvas.Model * shift);
                Thread.Sleep(1);
            }


            // set host game button to visible
            m_hostGameButton.SetVisible(true);
            m_joinGameButton.SetVisible(true);
            m_optionsButton.SetVisible(true);
            m_exitGameButton.SetVisible(true);
        }

        // Button delegates
        public void onHostGameClicked()
        {
            m_sceneManager.PushScene(m_hostGameScene);
        }

        public void joinGameButtonClicked()
        {
        }

        public void optionsButtonClicked()
        {
        }

        public void exitGameButtonClicked()
        {
            m_apiManager.GetWindowManager().CloseWindow();
        }

        bool m_rightButtonDown = false;
        bool m_leftButtonDown = false;
        FVec3 m_cameraPosition = new FVec3(-5.0f, -.5f, 3.0f);
        public void MouseRayEventHander(IEvent e)
        {
            // cast e to MouseEvent
            MouseEvent mouseEvent = e as MouseEvent;

            // Get ray from camera to mouse position on screen
            FVec3 start = FMat4.UnProject(Projection, Camera.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
            FVec3 end = FMat4.UnProject(Projection, Camera.View, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));

            //start = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(start.X(), start.Y(), start.Z(), 1.0f)).XYZ();
            //end = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(end.X(), end.Y(), end.Z(), 1.0f)).XYZ();

            m_mouseRay.UpdateLine(start, new FVec3(0, 0, 0));
            //m_mouseRay.UpdateLine(new FVec3(0, 0, 0), end);
        }
    }
}
