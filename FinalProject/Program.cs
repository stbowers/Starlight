using System;
using System.Collections.Generic;
using glfw3;
using FinalProject.Graphics.GLFW;
using FinalProject.Graphics.VK;
using FinalProject.Graphics;
using FinalProject.Graphics.SimpleRenderer;

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
			VulkanDriver driver = new VulkanDriver(window);

			// use simple renderer
			Renderer renderer = new SimpleVulkanRenderer(driver);

			// create sprite
			Graphics.Objects.Sprite spriteTest = new Graphics.Objects.Sprite(new GlmSharp.vec3(0.0f, 0.0f, 0.0f), "./sprite.png");
			renderer.AddObject(1, spriteTest);

			while (Glfw.WindowShouldClose(window.getWindow()) == 0)
			{
				Glfw.PollEvents();
			}
		}
	}
}
