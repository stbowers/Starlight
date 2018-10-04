using System;
using System.Diagnostics;
using StarlightEngine.Graphics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.GLFW;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Events;
using glfw3;

using StarlightGame.Graphics;
using StarlightGame.Graphics.Scenes;

namespace StarlightGame
{
	class MainClass
	{

		public static void Main(string[] args)
		{
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

			// set special objects for renderer
			IRendererSpecialObjectRefs specialObjectRefs = new IRendererSpecialObjectRefs();
			specialObjectRefs.fpsCounter = fpsText;
			specialObjectRefs.mousePositionCounter = mousePos.GetMousePosText();
			renderer.SetSpecialObjectReferences(specialObjectRefs);
			renderer.SetSpecialObjectsFlags(IRendererSpecialObjectFlags.RenderFPSCounter | IRendererSpecialObjectFlags.RenderMousePositionCounter);

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
                }
            }catch (Exception e)
            {
                Console.WriteLine("Exception caught: {0}\n" +
                                  "Stack Trace: {1}", e.Message, e.StackTrace);
            }

			eventManager.TerminateEventManagerAndJoin();

            Console.WriteLine("Closing Application");
		}


		public class MousePositionWrapper{
			VulkanAPIManager m_apiManager;
			VulkanTextObject m_mousePosText;
			public MousePositionWrapper(VulkanAPIManager apiManager, EventManager eventManager){
				// create mouse position indicator
				m_mousePosText = new VulkanTextObject(apiManager, StaticFonts.Font_Arial, "Mouse: (0.00, 0.00)", 20, new FVec2(-.99f, -.9f), 1.0f, true);

				eventManager.AddListener(MouseEvent, EventType.Mouse);
			}

			public void MouseEvent(IEvent e){
				FVec2 mousePos = (e as MouseEvent).MousePosition;
				m_mousePosText.UpdateText(StaticFonts.Font_Arial, string.Format("Mouse: ({0:0.##}, {1:0.##})", mousePos.X(), mousePos.Y()), 20);
			}

			public VulkanTextObject GetMousePosText(){
				return m_mousePosText;
			}
		}
	}
}
