using System.Threading;
using System.Diagnostics;
using StarlightEngine.Graphics.Vulkan;
using StarlightEngine.Graphics.Scenes;
using StarlightEngine.Graphics.Vulkan.Objects;
using StarlightEngine.Graphics.Math;
using StarlightEngine.Events;
using StarlightEngine.Graphics.Vulkan.Objects.Interfaces;

namespace StarlightGame.Graphics.Scenes
{
	public class TitleScene: Scene
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

		Vulkan3DLine m_xAxis;
		Vulkan3DLine m_yAxis;
		Vulkan3DLine m_zAxis;
		Vulkan3DLine m_mouseRay;

		VulkanBoxCollider m_buttonCollider;

		FMat4 m_projectionMatrix;
		FMat4 m_view;

		// Animation thread
		Thread m_animationThread;

		// Children scenes
		Scene m_hostGameScene;

		public TitleScene(VulkanAPIManager apiManager, SceneManager sceneManager, EventManager eventManager)
		{
			/* Layers:
			 * 	1: background
			 *  2: UI
			 */
			m_apiManager = apiManager;
			m_sceneManager = sceneManager;
			m_eventManager = eventManager;

			FMat4 model = FMat4.Rotate((0.0f / 1000.0f) * ((float)System.Math.PI / 4), new FVec3(0.0f, 1.0f, 0.0f));
			FMat4 view = FMat4.LookAt(new FVec3(0.0f, 0.0f, 2.0f), new FVec3(0.0f, 0.0f, 0.0f), new FVec3(0.0f, 1.0f, 0.0f));
			FMat4 proj = FMat4.Perspective((float)(75 * System.Math.PI) / 180, apiManager.GetSwapchainImageExtent().Width / apiManager.GetSwapchainImageExtent().Height, 0.1f, 100.0f);
			m_projectionMatrix = proj;
			m_view = view;

			m_canvas = new VulkanCanvas(new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f), new FVec2(2.0f, 2.0f));
			//m_canvas = new VulkanCanvas(model, new FVec2(2.0f, 2.0f), proj, view);

			FVec3 origin = new FVec3(0, 0, 0);
			m_xAxis = new Vulkan3DLine(apiManager, origin, new FVec3(100, 0, 0), new FVec4(1.0f, 0.0f, 0.0f, 1.0f), new FMat4(1.0f), view, proj);
			m_yAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 100, 0), new FVec4(0.0f, 1.0f, 0.0f, 1.0f), new FMat4(1.0f), view, proj);
			m_zAxis = new Vulkan3DLine(apiManager, origin, new FVec3(0, 0, 100), new FVec4(0.0f, 0.0f, 1.0f, 1.0f), new FMat4(1.0f), view, proj);
			m_mouseRay = new Vulkan3DLine(apiManager, origin, new FVec3(1, -10, 10), new FVec4(1.0f, 1.0f, .7f, 1.0f), new FMat4(1.0f), view, proj);
			m_eventManager.AddListener(MouseRayEventHander, EventType.Mouse);
			AddObject(0, m_xAxis);
			AddObject(0, m_yAxis);
			AddObject(0, m_zAxis);
			AddObject(0, m_mouseRay);

			m_background = new Vulkan2DSprite(apiManager, "./assets/bricks.jpg", new FVec2(-1.0f, -1.0f), new FVec2(2.0f, 2.0f));
			//AddObject(0, m_background);
			//m_canvas.AddObject(m_background);

			m_title = new Vulkan2DSprite(apiManager, "./assets/Title.png", new FVec2(-.75f, -.75f), new FVec2(1.5f, 1.0f), 0.0f);
			//AddObject(1, m_title);
			m_canvas.AddObject(m_title);

			float fill = 0.0f;
			m_loadingBar = new Vulkan2DProgressBar(apiManager, new FVec2(-.5f, .25f), new FVec2(1.0f, .1f), fill, new FVec4(1.0f, 1.0f, 1.0f, 1.0f));
			//AddObject(2, m_loadingBar);
			m_canvas.AddObject(m_loadingBar);

			// Host Game
			m_hostGameScene = new HostGameScene(m_apiManager, m_sceneManager, m_eventManager);
			m_hostGameButton = new VulkanUIButton(m_apiManager, StaticFonts.Font_Arial, "Host Game", 20, new FVec2(-.1f, 0.0f), new FVec2(.2f, .1f), onHostGameClicked);
			//AddObject(2, m_hostGameButton);
			m_canvas.AddObject(m_hostGameButton);
			m_hostGameButton.SetVisible(false);
			m_buttonCollider = m_hostGameButton.GetCollider();

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
		}

		// Button delegates
		public void onHostGameClicked(){
			m_sceneManager.PushScene(m_hostGameScene);
		}

		bool m_rightButtonDown = false;
		bool m_leftButtonDown = false;
		FVec3 m_cameraPosition = new FVec3(-5.0f, -.5f, 3.0f);
		public void MouseRayEventHander(IEvent e){
			// cast e to MouseEvent
			MouseEvent mouseEvent = e as MouseEvent;

			// Get ray from camera to mouse position on screen
			FVec3 start = FMat4.UnProject(m_projectionMatrix, m_view, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 0.0f));
			FVec3 end = FMat4.UnProject(m_projectionMatrix, m_view, new FVec3(mouseEvent.MousePosition.X(), mouseEvent.MousePosition.Y(), 1.0f));

			//start = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(start.X(), start.Y(), start.Z(), 1.0f)).XYZ();
			//end = (m_buttonCollider.Transform * FMat4.Invert(m_canvas.Model) * new FVec4(end.X(), end.Y(), end.Z(), 1.0f)).XYZ();

			m_mouseRay.UpdateLine(start, new FVec3(0, 0, 0));
			//m_mouseRay.UpdateLine(new FVec3(0, 0, 0), end);
		}
	}
}
