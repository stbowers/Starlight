using System;
using System.Collections.Generic;
using System.Diagnostics;
using StarlightEngine.Graphics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.GLFW;
using StarlightGame.Graphics;
using StarlightEngine.Graphics.Fonts;

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

			// create vulkan driver
            VulkanAPIManager apiManager = new VulkanAPIManager(window);

            // load shaders and pipelines
            StaticShaders.LoadAllShaders(apiManager);
            StaticPipelines.LoadAllPipelines(apiManager);

            // use simple renderer
            IRenderer renderer = new SimpleVulkanRenderer(apiManager);

            BasicVulkanTexturedObject obj = new BasicVulkanTexturedObject(apiManager, "./assets/dragon.obj", "./assets/bricks.jpg", StaticPipelines.pipeline_basic3D);
            renderer.AddObject(1, obj);

            // create fps counter
            AngelcodeFont arialFont = AngelcodeFontLoader.LoadFile("./assets/Arial.fnt");
            BasicVulkanTextObject textTest = new BasicVulkanTextObject(apiManager, StaticPipelines.pipeline_distanceFieldFont, arialFont, "FPS: 00.00", 20);
            renderer.AddObject(2, textTest);

            try
            {
                int framesDrawn = 0;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!window.ShouldWindowClose())
                {
                    window.PollEvents();
                    renderer.Update();
                    renderer.Render();
                    renderer.Present();
                    framesDrawn++;
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

            Console.WriteLine("Closing Application");
		}
	}
}
