﻿using System;
using System.Diagnostics;
using StarlightEngine.Graphics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.GLFW;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Events;
using StarlightEngine.Threadding;
using StarlightEngine.Interop;

using StarlightGame.Graphics;
using StarlightGame.Graphics.Scenes;


using StarlightEngine.Interop.glfw3;

using StarlightNetwork;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace StarlightGame
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            RNG.SeedRNG(DateTime.Now.GetHashCode());

            // Name main thread for debugging
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

            //NetworkManager networkManager = new NetworkManager();
            //string restURL;
            //List<GameServer> serverList;
            Console.WriteLine("Starting engine...");

            // ask user for the URL of the REST server
            //Console.WriteLine("URL of REST server: ");
            //restURL = Console.ReadLine();

            // get list of servers (we should clean restURL, but this is for testing so who cares)
            //serverList = networkManager.getServers(restURL);

            // ask user which server to use
            //for (int index = 0; index < serverList.Count; index++)
            //{
            //	GameServer server = serverList[index];
            //	Console.WriteLine("{0}) {1}:{2} - {3}", index, server.getURL(), server.getPort(), server.getName());
            //}
            //int serverIndex = Convert.ToInt32(Console.ReadLine());

            // get reference to server
            //GameServer gameServer = serverList[serverIndex];

            // create window
            GraphicsWindowGLFW window = new GraphicsWindowGLFW(1280, 720, "Starlight");

            // create event manager
            EventManager eventManager = new EventManager(window);

            // create vulkan manager
            VulkanAPIManager apiManager = new VulkanAPIManager(window);

            // load shaders and pipelines
            StaticShaders.LoadAllShaders(apiManager);
            StaticPipelines.LoadAllPipelines(apiManager);

            // use simple renderer
            IRenderer renderer = new SimpleVulkanRenderer(apiManager, StaticPipelines.pipeline_clear);
            SceneManager sceneManager = new SceneManager(renderer, eventManager);

            // create fps counter
            VulkanTextObject fpsText = new VulkanTextObject(apiManager, StaticFonts.Font_Arial, "FPS: 00.00", 20, new FVec2(-.99f, -.99f), 1.0f, true);

            // Mouse position tracker
            MousePositionWrapper mousePos = new MousePositionWrapper(apiManager, eventManager);

            // Debug overlay canvas
            VulkanCanvas debugCanvas = new VulkanCanvas(new FVec2(-1, -1), new FVec2(2, 2), new FVec2(2, 2));
            debugCanvas.UIScale = FMat4.Scale(new FVec3(2.0f / (float)apiManager.GetWindowManager().Width, 2.0f / (float)apiManager.GetWindowManager().Height, 1));

            debugCanvas.AddObject(fpsText);
            debugCanvas.AddObject(mousePos.GetMousePosText());

            // set special objects for renderer
            IRendererSpecialObjectRefs specialObjectRefs = new IRendererSpecialObjectRefs();
            specialObjectRefs.DebugOverlay = debugCanvas;
            renderer.SetSpecialObjectReferences(specialObjectRefs);
            renderer.SetSpecialObjectsFlags(IRendererSpecialObjectFlags.RenderDebugOverlay);

            // Add mouse cursor
            VulkanTextureCreateInfo mouseTextureInfo = new VulkanTextureCreateInfo();
            mouseTextureInfo.APIManager = apiManager;
            mouseTextureInfo.AnisotropyEnable = false;
            mouseTextureInfo.EnableMipmap = false;
            mouseTextureInfo.MinFilter = VulkanCore.Filter.Nearest;
            mouseTextureInfo.MagFilter = VulkanCore.Filter.Nearest;

            mouseTextureInfo.FileName = "./assets/Cursor-normal.png";
            VulkanTexture normalTexture = VulkanTextureCache.GetTexture(mouseTextureInfo.FileName, mouseTextureInfo);
            mouseTextureInfo.FileName = "./assets/Cursor-select.png";
            VulkanTexture selectCursor = VulkanTextureCache.GetTexture(mouseTextureInfo.FileName, mouseTextureInfo);
            mouseTextureInfo.FileName = "./assets/Cursor-loading.png";
            VulkanTexture loadingCursor = VulkanTextureCache.GetTexture(mouseTextureInfo.FileName, mouseTextureInfo);

            float mouseWidth = 64.0f / 1280.0f;
            float mouseHeight = 64.0f / 720.0f;
            Vulkan2DSprite mouseSprite = new Vulkan2DSprite(apiManager, normalTexture, new FVec2(0.0f, 0.0f), new FVec2(1.3f * mouseWidth, 1.3f * mouseHeight));
            debugCanvas.AddObject(mouseSprite);

            eventManager.Subscribe(EngineEvent.SetMouseNormal, (object sender, IEvent e) =>
            {
                mouseSprite.UpdateTexture(normalTexture);
            });

            eventManager.Subscribe(EngineEvent.SetMouseSelect, (object sender, IEvent e) =>
            {
                mouseSprite.UpdateTexture(selectCursor);
            });

            eventManager.Subscribe(EngineEvent.SetMouseLoading, (object sender, IEvent e) =>
            {
                mouseSprite.UpdateTexture(loadingCursor);
            });

            eventManager.Subscribe(MouseEvent.ID, (object sender, IEvent e) =>
            {
                MouseEvent mouseEvent = e as MouseEvent;
                mouseSprite.UpdatePositionScale(mouseEvent.MousePosition, new FVec2(1.3f * mouseWidth, 1.3f * mouseHeight));
            });

            // Set up title scene
            Scene titleScene = new TitleScene(apiManager, sceneManager, eventManager);
            sceneManager.PushScene(titleScene);

            // Run game loop
            try
            {
                int framesDrawn = 0;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!window.ShouldWindowClose())
                {
                    GLFWNativeFunctions.glfwPollEvents();
                    renderer.Update();
                    renderer.Render();
                    renderer.Present();
                    framesDrawn++;

                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        long msElapsed = sw.ElapsedMilliseconds;
                        sw.Restart();
                        fpsText.UpdateText(StaticFonts.Font_Arial, string.Format("FPS: {0:0.##}", ((framesDrawn * 1000L) / (float)msElapsed)), 20);
                        framesDrawn = 0;
                    }

                    // Check for console input
                    if (Console.KeyAvailable)
                    {
                        string input = Console.ReadLine();
                        EngineEvent e = new EngineEvent();
                        e.Data = input;
                        eventManager.Notify(EngineEvent.CommandSentID, Console.In, e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: {0}\n" +
                                  "Stack Trace: {1}", e.Message, e.StackTrace);
            }

            Console.WriteLine("Closing Application");
        }

        public class MousePositionWrapper
        {
            VulkanAPIManager m_apiManager;
            VulkanTextObject m_mousePosText;
            public MousePositionWrapper(VulkanAPIManager apiManager, EventManager eventManager)
            {
                // create mouse position indicator
                m_mousePosText = new VulkanTextObject(apiManager, StaticFonts.Font_Arial, "Mouse: (0.00, 0.00)", 20, new FVec2(-.99f, -.9f), 1.0f, true);

                //eventManager.AddListener(MouseEvent, EventType.Mouse);
                eventManager.Subscribe(MouseEvent.ID, MouseEventSubscriber);
            }

            public void MouseEventSubscriber(object sender, IEvent e)
            {
                FVec2 mousePos = (e as MouseEvent).MousePosition;
                m_mousePosText.UpdateText(StaticFonts.Font_Arial, string.Format("Mouse: ({0:0.##}, {1:0.##})", mousePos.X(), mousePos.Y()), 20);
            }

            public VulkanTextObject GetMousePosText()
            {
                return m_mousePosText;
            }
        }
    }
}
