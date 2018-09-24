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
			GraphicsWindowGLFW window = new GraphicsWindowGLFW(1280, 720, "C# Game");

			// create event manager
			EventManager eventManager = new EventManager(window);
			EventListener listener = new EventListener();
			listener.filter = EventType.Keyboard;
			listener.handler = EventHandler;
			eventManager.AddListener(listener);

			// create vulkan driver
            VulkanAPIManager apiManager = new VulkanAPIManager(window);

            // load shaders and pipelines
            StaticShaders.LoadAllShaders(apiManager);
            StaticPipelines.LoadAllPipelines(apiManager);

            // use simple renderer
			IRenderer renderer = new SimpleVulkanRenderer(apiManager, StaticPipelines.pipeline_clear);
			SceneManager sceneManager = new SceneManager(renderer);

			// Title screen
			Scene titleScene = new Scene();

			FMat4 model = FMat4.Rotate((0.0f / 1000.0f) * ((float)System.Math.PI / 4), new FVec3(0.0f, 1.0f, 0.0f));
			FMat4 view = FMat4.LookAt(new FVec3(10.0f, 10.0f, 10.0f), new FVec3(0.0f, 4.0f, 0.0f), new FVec3(0.0f, 1.0f, 0.0f));
			FMat4 proj = FMat4.Perspective((float)(75 * System.Math.PI) / 180, apiManager.GetSwapchainImageExtent().Width / apiManager.GetSwapchainImageExtent().Height, 0.01f, 100.0f);
			proj[1, 1] *= -1f;
			//AnimatedModel obj = new AnimatedModel(apiManager, StaticPipelines.pipeline_basic3D, "./assets/dragon.obj", "./assets/bricks.jpg", model, view, proj);
			FVec4 lightPosition = new FVec4(0.0f, 0.0f, 1000.0f, 0.0f);
			FVec4 lightColor = new FVec4(1.0f, 1.0f, 1.0f, 0.0f);
			float[] settings = { 0.15f, 50.0f, 1.0f };

			VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe kf1 = new VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe();
			kf1.Translation = new FMat4(1.0f);
			kf1.Rotation = Quaternion.Rotate(0, new FVec3(0.0f, 1.0f, 0.0f));
			kf1.time = 0.0f;
			VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe kf2 = new VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe();
			kf2.Translation = FMat4.Translate(new FVec3(0.0f, 0.0f, 10.0f));// * Mat4.Rotate((float)System.Math.PI / 2, new Vec3(0.0f, 1.0f, 0.0f));
			kf2.Rotation = Quaternion.Rotate((float)System.Math.PI / 2, new FVec3(0.0f, 1.0f, 0.0f));
			kf2.time = 2.0f;
			VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe kf3 = new VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe();
			kf3.Translation = FMat4.Translate(new FVec3(10.0f, 0.0f, 0.0f));// * Mat4.Rotate((float)System.Math.PI / 2, new Vec3(0.0f, 1.0f, 0.0f));
			kf3.Rotation = Quaternion.Rotate((float)System.Math.PI, new FVec3(0.0f, 1.0f, 0.0f));
			kf3.time = 4.0f;
			VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe kf4 = new VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe();
			kf4.Translation = FMat4.Translate(new FVec3(0.0f, 0.0f, 0.0f));// * Mat4.Rotate((float)System.Math.PI / 2, new Vec3(0.0f, 1.0f, 0.0f));
			kf4.Rotation = Quaternion.Rotate(2.0f * (float)System.Math.PI, new FVec3(0.0f, 1.0f, 0.0f));
			kf4.time = 8.0f;
			VulkanSimpleAnimatedTexturedMesh.AnimationKeyframe[] keyframes = new[] { kf1, kf2, kf3, kf4 };

			VulkanSimpleAnimatedTexturedMesh obj = new VulkanSimpleAnimatedTexturedMesh(apiManager, "./assets/dragon.obj", "./assets/bricks.jpg", keyframes, view, proj, lightPosition, lightColor, settings[0], settings[1], settings[2]);
			titleScene.AddObject(1, obj);

            // create fps counter
            AngelcodeFont arialFont = AngelcodeFontLoader.LoadFile("./assets/Arial.fnt");
			VulkanTextObject textTest = new VulkanTextObject(apiManager, arialFont, "FPS: 00.00", 20, new FVec2(-.99f, -.99f), 1.0f);
			titleScene.AddObject(2, textTest);

			// create mouse position indicator
			VulkanTextObject mousePosText = new VulkanTextObject(apiManager, arialFont, "Mouse: (0.00, 0.00)", 20, new FVec2(-.99f, -.9f), 1.0f);
			titleScene.AddObject(2, mousePosText);

			Vulkan2DSprite title = new Vulkan2DSprite(apiManager, "./assets/Title.png", new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f));
			titleScene.AddObject(3, title);

			float fill = 0.0f;
			Vulkan2DProgressBar loadBar = new Vulkan2DProgressBar(apiManager, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
			titleScene.AddObject(4, loadBar);

            // status recolorable asset
            FVec4 from1 = new FVec4(1.0f, 1.0f, 1.0f, 0.0f);
            FVec4 to1 = new FVec4(1.0f, 0.0f, 0.0f, 0.0f);
            FVec4 from2 = new FVec4(0.0f, 0.0f, 0.0f, 0.0f);
            FVec4 to2 = new FVec4(0.0f, 1.0f, 0.0f, 0.0f);

            VulkanRecolorable2DSprite status = new VulkanRecolorable2DSprite(apiManager, "./assets/Indicator.png", new FVec2(.5f, .5f), new FVec2(.2f, .2f), from1, to1, from2, to2);
			titleScene.AddObject(5, status);

			// new game
			Scene newGameScene = new Scene();
			newGameScene.AddObject(1, textTest);
			newGameScene.AddObject(1, mousePosText);
			StartGameButtonWrapper button = new StartGameButtonWrapper(apiManager, titleScene, arialFont, sceneManager, newGameScene);

			Vulkan2DRect rect = new Vulkan2DRect(apiManager, new FVec2(.1f, .1f), new FVec2(.1f, .1f), new FVec4(1.0f, 0.0f, 0.0f, 1.0f));
			titleScene.AddObject(7, rect);

			VulkanBoxCollider boxCollider = new VulkanBoxCollider(new FVec2(0.5f, 0.5f), new FVec2(.3f, .3f));
			bool isIn = boxCollider.IsCollision(new FVec3(.6f, 1.6f, 0.0f));
			Console.WriteLine("Point is {0}", isIn? "in" : "not in");

			sceneManager.PushScene(titleScene);
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

					if (spacePressed)
					{
						obj.PlayAnimation();
						fill += .0003f;
						loadBar.UpdatePercentage(fill);
					}
					else
					{
						obj.PauseAnimation();
					}

					FVec2 mousePos = window.GetMousePosition();
					mousePosText.UpdateText(arialFont, string.Format("Mouse: ({0:0.##}, {1:0.##})", mousePos.X, mousePos.Y), 20);

                    if (sw.ElapsedMilliseconds > 1000)
                    {
						long msElapsed = sw.ElapsedMilliseconds;
                        sw.Restart();
                        textTest.UpdateText(arialFont, string.Format("FPS: {0:0.##}", ((framesDrawn * 1000L) / (float)msElapsed)), 20);
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

		static bool spacePressed = false;

		static void EventHandler(IEvent @event)
		{
			if (@event.Type.HasFlag(EventType.Keyboard))
			{
				KeyboardEvent keyboardEvent = @event as KeyboardEvent;
				if (keyboardEvent.key == Key.Space)
				{
					if (keyboardEvent.action == KeyAction.Press || keyboardEvent.action == KeyAction.Repeat)
					{
						spacePressed = true;
					}
					else
					{
						spacePressed = false;
					}
				}
			}
		}

		public class StartGameButtonWrapper
		{
			VulkanUIButton m_button;
			SceneManager m_sceneManager;
			Scene m_newGameScene;

			public StartGameButtonWrapper(VulkanAPIManager apiManager, Scene titleScene, AngelcodeFont arialFont, SceneManager sceneManager, Scene newGameScene)
			{
				m_newGameScene = newGameScene;
				m_sceneManager = sceneManager;
				VulkanUIButton button = new VulkanUIButton(apiManager, arialFont, "Start Game", 20, new FVec2(.1f, 0.0f), new FVec2(.3f, .1f), Clicked);
				titleScene.AddObject(6, button);
			}

			public void Clicked()
			{
				// push new game scene
				m_sceneManager.PushScene(m_newGameScene);
			}
		}
	}
}
