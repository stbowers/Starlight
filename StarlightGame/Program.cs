using System;
using System.Collections.Generic;
using System.Diagnostics;
using StarlightEngine.Graphics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.GLFW;
using StarlightGame.Graphics;
using StarlightEngine.Graphics.Fonts;
using StarlightEngine.Graphics.Math;
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

			//VulkanSimpleAnimatedTexturedMesh obj = new VulkanSimpleAnimatedTexturedMesh(apiManager, StaticPipelines.pipeline_basic3D, "./assets/dragon.obj", "./assets/bricks.jpg", keyframes, view, proj, lightPosition, lightColor, settings[0], settings[1], settings[2]);
            //renderer.AddObject(1, obj);

            // create fps counter
            AngelcodeFont arialFont = AngelcodeFontLoader.LoadFile("./assets/Arial.fnt");
			VulkanTextObject textTest = new VulkanTextObject(apiManager, StaticPipelines.pipeline_distanceFieldFont, arialFont, "FPS: 00.00", 20, new FVec2(-638.0f, -357.0f), 640.0f);
            renderer.AddObject(2, textTest);

			Vulkan2DSprite title = new Vulkan2DSprite(apiManager, StaticPipelines.pipeline_basic2D, "./assets/Title.png", new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f));
			renderer.AddObject(3, title);

			float fill = 0.0f;
			Vulkan2DProgressBar loadBar = new Vulkan2DProgressBar(apiManager, StaticPipelines.pipeline_colorLine, StaticPipelines.pipeline_color2D, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
			renderer.AddObject(4, loadBar);

            // status recolorable asset
            FVec4 from1 = new FVec4(1.0f, 1.0f, 1.0f, 0.0f);
            FVec4 to1 = new FVec4(1.0f, 0.0f, 0.0f, 0.0f);
            FVec4 from2 = new FVec4(0.0f, 0.0f, 0.0f, 0.0f);
            FVec4 to2 = new FVec4(0.0f, 1.0f, 0.0f, 0.0f);

            VulkanRecolorable2DSprite status = new VulkanRecolorable2DSprite(apiManager, StaticPipelines.pipeline_recolor2D, "./assets/Indicator.png", new FVec2(.5f, .5f), new FVec2(.2f, .2f), from1, to1, from2, to2);
            renderer.AddObject(5, status);

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
						//obj.PlayAnimation();
						fill += .0003f;
						loadBar.UpdatePercentage(fill);
					}
					else
					{
						//obj.PauseAnimation();
					}

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
	}
}
