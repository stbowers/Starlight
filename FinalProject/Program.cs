using System;
using System.Collections.Generic;
using System.Diagnostics;
using glfw3;
using FinalProject.Graphics;
using FinalProject.Graphics.GLFW;
using FinalProject.Graphics.Vulkan;
using FinalProject.Graphics.Vulkan.Objects;

namespace FinalProject
{
	class MainClass
	{

		public static void Main(string[] args)
		{
			NetworkManager networkManager = new NetworkManager();
			string restURL;
			List<GameServer> serverList;
			Console.WriteLine("Starting engine...");

			// ask user for the URL of the REST server
			Console.WriteLine("URL of REST server: ");
			restURL = Console.ReadLine();

			// get list of servers (we should clean restURL, but this is for testing so who cares)
			serverList = networkManager.getServers(restURL);

			// ask user which server to use
			for (int index = 0; index < serverList.Count; index++)
			{
				GameServer server = serverList[index];
				Console.WriteLine("{0}) {1}:{2} - {3}", index, server.getURL(), server.getPort(), server.getName());
			}
			int serverIndex = Convert.ToInt32(Console.ReadLine());

			// get reference to server
			GameServer gameServer = serverList[serverIndex];

			// create window
			GraphicsWindowGLFW window = new GraphicsWindowGLFW(1280, 720, "C# Game");

			// create vulkan driver
			try
			{
				VulkanAPIManager apiManager = new VulkanAPIManager(window);

				// use simple renderer
				IRenderer renderer = new SimpleVulkanRenderer(apiManager);

				BasicVulkanTexturedObject obj = new BasicVulkanTexturedObject(apiManager, "./assets/dragon.obj", "./assets/objtexture.png");
				renderer.AddObject(1, obj);

				int framesDrawn = 0;
				Stopwatch sw = new Stopwatch();
				sw.Start();
				while (Glfw.WindowShouldClose(window.getWindow()) == 0)
				{
					Glfw.PollEvents();
					renderer.Update();
					renderer.Render();
					renderer.Present();
					framesDrawn++;
					if (framesDrawn >= 600)
					{
						long msElapsed = sw.ElapsedMilliseconds;
						sw.Restart();
						Console.WriteLine("average fps: {0}", ((framesDrawn * 1000L) / (float)msElapsed));
						framesDrawn = 0;
					}
				}

                Console.WriteLine("Closing Application");
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception thrown: {0}\nStack Trace: {1}", e.Message, e.StackTrace);
            }
		}
	}
}
