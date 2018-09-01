using System;
using System.Collections.Generic;
using Vulkan;
using System.Runtime.InteropServices;

namespace FinalProject.Graphics.VK
{
	public class ExtensionNotAvailableException : Exception
	{
	}
	public class LayerNotAvailableException : Exception
	{
	}

	public class VulkanDriver : Driver
	{
		private class DeviceQueueFamilies
		{
			public int graphicsFamily = -1;
			public int transferFamily = -1;
			public int presentFamily = -1;

			public bool isComplete()
			{
				return (
					(graphicsFamily > 0) &&
					(transferFamily > 0) &&
					(presentFamily > 0)
				);
			}
		}

		private struct SwapChainSupportDetails
		{
			public Vulkan.VkSurfaceCapabilitiesKHR capabilities;
			public Vulkan.VkSurfaceFormatKHR[] formats;
			public Vulkan.VkPresentModeKHR[] presentModes;
		}

		private Vulkan.VkInstance instance;
		private Vulkan.VkSurfaceKHR surface;
		private int windowWidth, windowHeight;
		private Vulkan.VkDebugReportCallbackEXT debugReportCallback;
		private Vulkan.VkPhysicalDevice physicalDevice;
		private Vulkan.VkDevice device;
		private VulkanMemoryAllocator memoryAllocator;
		private DeviceQueueFamilies queueFamilies;
		private Vulkan.VkQueue graphicsQueue;
		private Vulkan.VkQueue transferQueue;
		private Vulkan.VkQueue presentQueue;
		private Vulkan.VkCommandPool graphicsCommandPool;
		private uint graphicsQueueIndex;
		private Vulkan.VkCommandPool transferCommandPool;
		private uint transferQueueIndex;
		private Vulkan.VkSwapchainKHR swapchain; // create a new swapchain during initialization, so that the code can be generalized to recreate the swapchain
		private Vulkan.VkFormat swapchainImageFormat;
		private Vulkan.VkFormat depthImageFormat;
		private Vulkan.VkExtent2D swapchainImageExtent;
		private Vulkan.VkImage[] swapchainImages;
		private Vulkan.VkImageView[] swapchainImageViews;
		private VkImage depthImage;
		private VkImageView depthImageView;
		private VmaAllocation depthImageAllocation;
		private VkCommandBuffer[] swapchainCommandBuffers;
		private VkSemaphore[] imageAvailableSemaphores;
		private VkSemaphore[] renderFinishedSemaphores;
		private VkFence[] inFlightFences;

		public unsafe VulkanDriver(Window window)
		{
			windowWidth = window.GetWidth();
			windowHeight = window.GetHeight();

			createInstance(window);

			getPhysicalDevice();

			byte*[] deviceExtensions = { };
			byte*[] deviceLayers = { };
			createLogicalDevice(deviceLayers, deviceExtensions);

			createSwapChain();
		}

		unsafe ~VulkanDriver()
		{
			Vulkan.VulkanNative.vkDestroyCommandPool(device, graphicsCommandPool, null);
			Vulkan.VulkanNative.vkDestroyCommandPool(device, transferCommandPool, null);

			Vulkan.VulkanNative.vkDestroyDevice(device, null);

			Vulkan.VulkanNative.vkDestroyDebugReportCallbackEXT(instance, debugReportCallback, null);
			Vulkan.VulkanNative.vkDestroyInstance(instance, null);
		}

		unsafe private void createInstance(Window window,
									List<string> requiredExtensions = null, List<string> requestedExtensions = null,
									   List<string> requiredLayers = null, List<string> requestedLayers = null)
		{
			Vulkan.VkApplicationInfo appInfo = new Vulkan.VkApplicationInfo();
			Vulkan.VkInstanceCreateInfo createInfo = new Vulkan.VkInstanceCreateInfo();
			List<string> extensions = new List<string>();
			List<string> layers = new List<string>();
			Vulkan.VkExtensionProperties[] availableExtensions;
			Vulkan.VkLayerProperties[] availableLayers;

			// Get list of extensions and layers available to us
			uint nAvailableExtensions;
			uint nAvailableLayers;
			Vulkan.VulkanNative.vkEnumerateInstanceExtensionProperties((byte*)null, &nAvailableExtensions, null);
			Vulkan.VulkanNative.vkEnumerateInstanceLayerProperties(&nAvailableLayers, null);
			availableExtensions = new Vulkan.VkExtensionProperties[nAvailableExtensions];
			availableLayers = new Vulkan.VkLayerProperties[nAvailableLayers];
			fixed(Vulkan.VkExtensionProperties* pAvailableExtensions = availableExtensions)
			fixed(Vulkan.VkLayerProperties* pAvailableLayers = availableLayers)
			{
				Vulkan.VulkanNative.vkEnumerateInstanceExtensionProperties((byte*)null, &nAvailableExtensions, pAvailableExtensions);
				Vulkan.VulkanNative.vkEnumerateInstanceLayerProperties(&nAvailableLayers, pAvailableLayers);
			}

			// Get list of extensions required to draw to the surface
			string[] requiredSurfaceExtensions = window.GetVulkanExtensions();

			// List of required extensions for validation layers
			List<string> validationExtensions = new List<string>();
#if DEBUG
			validationExtensions.Add("VK_EXT_debug_utils");
			validationExtensions.Add("VK_EXT_debug_report");
#endif

			// Check that each required extension is supported and add it to the list of extensions to load
			HashSet<string> requiredVulkanExtensions = new HashSet<string>();
			if (requiredExtensions != null) requiredVulkanExtensions.UnionWith(requiredExtensions);
			requiredVulkanExtensions.UnionWith(requiredSurfaceExtensions);
			requiredVulkanExtensions.UnionWith(validationExtensions);
			foreach (string requiredExtension in requiredVulkanExtensions)
			{
				bool available = false;
				for (int i = 0; i < availableExtensions.Length; i++)
				{
					fixed (byte* name = availableExtensions[i].extensionName)
					{
						if (Utilities.BytePointerToString(name).Equals(requiredExtension))
						{
							available = true;
						}
					}
				}
				if (available)
				{
					Console.WriteLine("Vulkan Extension \"{0}\" Loaded.", requiredExtension);
					extensions.Add(requiredExtension);
				}
				else
				{
					Console.WriteLine("Required Vulkan Extension \"{0}\" is not available, aborting...", requiredExtension);
					throw new ExtensionNotAvailableException();
				}
			}

			// Add any requested extensions which are supported to the list of extensions to load
			HashSet<string> requestedVulkanExtensions = new HashSet<string>();
			if (requestedExtensions != null) requestedVulkanExtensions.UnionWith(requestedExtensions);
			foreach (string requestedExtension in requestedVulkanExtensions)
			{
				bool available = false;
				for (int i = 0; i < availableExtensions.Length; i++)
				{
					fixed (byte* name = availableExtensions[i].extensionName)
					{
						if (Utilities.BytePointerToString(name).Equals(requestedExtension))
						{
							available = true;
						}
					}
				}
				if (available)
				{
					Console.WriteLine("Vulkan Extension \"{0}\" Loaded.", requestedExtension);
					extensions.Add(requestedExtension);
				}
				else
				{
					Console.WriteLine("Vulkan Extension \"{0}\" is not available, skipping...", requestedExtension);
				}
			}

			// Create list of validation layers based on if we're in debug or release mode
			List<string> validationLayers = new List<string>();
#if DEBUG
			validationLayers.Add("VK_LAYER_LUNARG_standard_validation");
#endif

			// Check that each required layer is supported and add it to the list of layers to load
			HashSet<string> requiredVulkanLayers = new HashSet<string>();
			if (requiredLayers != null) requiredVulkanLayers.UnionWith(requiredLayers);
			foreach (string requiredLayer in requiredVulkanLayers)
			{
				bool available = false;
				for (int i = 0; i < availableLayers.Length; i++)
				{
					fixed (byte* name = availableLayers[i].layerName)
					{
						if (Utilities.BytePointerToString(name).Equals(requiredLayer))
						{
							available = true;
						}
					}
				}
				if (available)
				{
					Console.WriteLine("Vulkan Layer \"{0}\" Loaded.", requiredLayer);
					layers.Add(requiredLayer);
				}
				else
				{
					Console.WriteLine("Required Vulkan Layer \"{0}\" is not available, aborting...", requiredLayer);
					throw new LayerNotAvailableException();
				}
			}

			// Add any requested layers which are supported to the list of layers to load
			HashSet<string> requestedVulkanLayers = new HashSet<string>();
			if (requestedLayers != null) requestedVulkanLayers.UnionWith(requestedLayers);
			requestedVulkanLayers.UnionWith(validationLayers);
			foreach (string requestedLayer in requestedVulkanLayers)
			{
				bool available = false;
				for (int i = 0; i < availableLayers.Length; i++)
				{
					fixed (byte* name = availableLayers[i].layerName)
					{
						if (Utilities.BytePointerToString(name).Equals(requestedLayer))
						{
							available = true;
						}
					}
				}
				if (available)
				{
					Console.WriteLine("Vulkan Layer \"{0}\" Loaded.", requestedLayer);
					layers.Add(requestedLayer);
				}
				else
				{
					Console.WriteLine("Vulkan Layer \"{0}\" is not available, skipping...", requestedLayer);
				}
			}

			// Create arrays of byte* strings for extensions and layers to enable
			byte*[] paEnabledExtensions = new byte*[extensions.Count];
			byte*[] paEnabledLayers = new byte*[layers.Count];
			for (int i = 0; i < extensions.Count; i++)
			{
				fixed (byte* extension = Utilities.StringToByteArray(extensions[i]))
				{
					paEnabledExtensions[i] = extension;
				}
			}
			for (int i = 0; i < layers.Count; i++)
			{
				fixed (byte* layer = Utilities.StringToByteArray(layers[i]))
				{
					paEnabledLayers[i] = layer;
				}
			}

			byte[] appName = Utilities.StringToByteArray("Name");
			byte[] engineName = Utilities.StringToByteArray("No Engine");

			fixed (byte* pAppName = appName)
			fixed (byte* pEngineName = engineName)
			fixed (byte** ppEnabledExtensions = paEnabledExtensions)
			fixed (byte** ppEnabledLayers = paEnabledLayers)
			fixed (Vulkan.VkInstance* pInstance = &instance)
			{
				// Construct ApplicationInfo struct
				appInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_APPLICATION_INFO;
				appInfo.pApplicationName = pAppName;
				appInfo.applicationVersion = Utilities.MakeVersionNumber(1, 0, 0);
				appInfo.pEngineName = pEngineName;
				appInfo.engineVersion = Utilities.MakeVersionNumber(1, 0, 0);
				appInfo.apiVersion = Utilities.MakeVersionNumber(1, 0, 0);

				// Construct InstanceCreateInfo struct
				createInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
				createInfo.pApplicationInfo = &appInfo;
				createInfo.enabledExtensionCount = (uint)extensions.Count;
				createInfo.ppEnabledExtensionNames = ppEnabledExtensions;
				createInfo.enabledLayerCount = (uint)layers.Count;
				createInfo.ppEnabledLayerNames = ppEnabledLayers;

				// Create Vulkan instance
				Vulkan.VkResult result = Vulkan.VulkanNative.vkCreateInstance(&createInfo, null, pInstance);
				if (result != Vulkan.RawConstants.VK_SUCCESS)
				{
					switch (result)
					{
						case Vulkan.RawConstants.VK_ERROR_OUT_OF_HOST_MEMORY:
							Console.WriteLine("Failed to create instance: out of host memory");
							break;
						case Vulkan.RawConstants.VK_ERROR_OUT_OF_DEVICE_MEMORY:
							Console.WriteLine("Failed to create instance: out of device memory");
							break;
						case Vulkan.RawConstants.VK_ERROR_INITIALIZATION_FAILED:
							Console.WriteLine("Failed to create instance: initialization failed");
							break;
						case Vulkan.RawConstants.VK_ERROR_LAYER_NOT_PRESENT:
							Console.WriteLine("Failed to create instance: layer not present");
							break;
						case Vulkan.RawConstants.VK_ERROR_EXTENSION_NOT_PRESENT:
							Console.WriteLine("Failed to create instance: extension not present");
							break;
						case Vulkan.RawConstants.VK_ERROR_INCOMPATIBLE_DRIVER:
							Console.WriteLine("Failed to create instance: incompatable driver");
							break;
					}
					throw new ExternalException();
				}

				surface = window.GetVulkanSurface(instance);



				Vulkan.VkDebugReportCallbackCreateInfoEXT debugCallbackCreateInfo = new Vulkan.VkDebugReportCallbackCreateInfoEXT();
				Vulkan.FunctionPointer<PFN_vkDebugReportCallbackEXT> debugCallabackPfn = new FunctionPointer<PFN_vkDebugReportCallbackEXT>(new PFN_vkDebugReportCallbackEXT(DebugReportCallback));
				debugCallbackCreateInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_DEBUG_REPORT_CALLBACK_CREATE_INFO_EXT;
				debugCallbackCreateInfo.pfnCallback = debugCallabackPfn.Pointer;
				debugCallbackCreateInfo.flags = Vulkan.VkDebugReportFlagsEXT.ErrorEXT | Vulkan.VkDebugReportFlagsEXT.WarningEXT | Vulkan.VkDebugReportFlagsEXT.PerformanceWarningEXT | Vulkan.VkDebugReportFlagsEXT.InformationEXT | Vulkan.VkDebugReportFlagsEXT.DebugEXT;

				#if DEBUG
					fixed (Vulkan.VkDebugReportCallbackEXT* pDebugReportCallback = &debugReportCallback)
					{
						Vulkan.VulkanNative.vkCreateDebugReportCallbackEXT(instance, &debugCallbackCreateInfo, null, pDebugReportCallback);
					}
				#endif
			}
		}

		private unsafe void getPhysicalDevice()
		{
			Vulkan.VkPhysicalDevice[] physicalDevices;
			unsafe
			{
				uint nPhysicalDevices = 0;
				Vulkan.VulkanNative.vkEnumeratePhysicalDevices(instance, &nPhysicalDevices, null);
				physicalDevices = new Vulkan.VkPhysicalDevice[nPhysicalDevices];
				fixed(Vulkan.VkPhysicalDevice* pPhysicalDevices = physicalDevices)
				{
					Vulkan.VulkanNative.vkEnumeratePhysicalDevices(instance, &nPhysicalDevices, pPhysicalDevices);
				}
			}

			Console.WriteLine("{0} Vulkan-capable devices found", physicalDevices.Length);
			if (physicalDevices.Length == 0)
			{
				throw new NotSupportedException();
			}

			physicalDevice = choosePhysicalDevice(physicalDevices, surface);

			queueFamilies = getDeviceQueueFamilies(physicalDevice, surface);

			Vulkan.VkPhysicalDeviceProperties deviceProperties;
			Vulkan.VulkanNative.vkGetPhysicalDeviceProperties(physicalDevice, &deviceProperties);

			Console.WriteLine("Using device: {0}", Utilities.BytePointerToString(deviceProperties.deviceName));
		}

		private static Vulkan.VkPhysicalDevice choosePhysicalDevice(Vulkan.VkPhysicalDevice[] devices, VkSurfaceKHR surface)
		{
			foreach (Vulkan.VkPhysicalDevice physicalDevice in devices)
			{
				DeviceQueueFamilies deviceQueueFamilies = getDeviceQueueFamilies(physicalDevice, surface);
				if (deviceQueueFamilies.isComplete())
				{
					return physicalDevice;
				}
			}
			return devices[0];
		}

		private static DeviceQueueFamilies getDeviceQueueFamilies(Vulkan.VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
		{
			DeviceQueueFamilies indicies = new DeviceQueueFamilies();

			Vulkan.VkQueueFamilyProperties[] queueFamilyProperties;
			unsafe
			{
				uint nQueueFamilyProperties;
				Vulkan.VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &nQueueFamilyProperties, null);
				queueFamilyProperties = new Vulkan.VkQueueFamilyProperties[nQueueFamilyProperties];
				fixed (Vulkan.VkQueueFamilyProperties* pQueueFamilyProperties = queueFamilyProperties)
				{
					Vulkan.VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &nQueueFamilyProperties, pQueueFamilyProperties);
				}
			}

			int i = 0;
			foreach (Vulkan.VkQueueFamilyProperties queueFamily in queueFamilyProperties)
			{
				if (queueFamily.queueCount > 0 && (queueFamily.queueFlags & Vulkan.RawConstants.VK_QUEUE_GRAPHICS_BIT) != 0)
				{
					indicies.graphicsFamily = i;
				}

				if (queueFamily.queueCount > 0 && (queueFamily.queueFlags & Vulkan.RawConstants.VK_QUEUE_TRANSFER_BIT) != 0)
				{
					indicies.transferFamily = i;
				}

				Vulkan.VkBool32 presentSupport = false;
				unsafe
				{
					Vulkan.VulkanNative.vkGetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)i, surface, &presentSupport);
				}
				if (queueFamily.queueCount > 0 && presentSupport)
				{
					indicies.presentFamily = i;
				}

				if (indicies.isComplete())
				{
					break;
				}
				i++;
			}

			return indicies;
		}

		private unsafe void createLogicalDevice(byte*[] deviceLayers, byte*[] deviceExtensions)
		{
			Vulkan.VkDeviceQueueCreateInfo[] queueCreateInfos;
			ISet<uint> uniqueQueueFamilies = new HashSet<uint>();
			uniqueQueueFamilies.Add((uint)queueFamilies.graphicsFamily);
			uniqueQueueFamilies.Add((uint)queueFamilies.transferFamily);
			uniqueQueueFamilies.Add((uint)queueFamilies.presentFamily);
			queueCreateInfos = new Vulkan.VkDeviceQueueCreateInfo[uniqueQueueFamilies.Count];

			float queuePriorityNormal = 1.0f;
			int i = 0;
			foreach (uint queueFamily in uniqueQueueFamilies)
			{
				Vulkan.VkDeviceQueueCreateInfo queueCreateInfo = new Vulkan.VkDeviceQueueCreateInfo();
				queueCreateInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
				queueCreateInfo.queueFamilyIndex = queueFamily;
				queueCreateInfo.queueCount = 1;
				queueCreateInfo.pQueuePriorities = &queuePriorityNormal;

				queueCreateInfos[i] = queueCreateInfo;
				i++;
			}

			Vulkan.VkPhysicalDeviceFeatures deviceFeatures = new Vulkan.VkPhysicalDeviceFeatures();
			deviceFeatures.samplerAnisotropy = Vulkan.RawConstants.VK_TRUE;

			fixed (Vulkan.VkDeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
			fixed (byte** pDeviceLayers = deviceLayers)
			fixed (byte** pDeviceExtensions = deviceExtensions)
			fixed (Vulkan.VkDevice* pDevice = &device)
			{
				Vulkan.VkDeviceCreateInfo logicalDeviceCreateInfo = new Vulkan.VkDeviceCreateInfo();
				logicalDeviceCreateInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
				logicalDeviceCreateInfo.queueCreateInfoCount = (uint)queueCreateInfos.Length;
				logicalDeviceCreateInfo.pQueueCreateInfos = pQueueCreateInfos;
				logicalDeviceCreateInfo.enabledLayerCount = (uint)deviceLayers.Length;
				logicalDeviceCreateInfo.ppEnabledLayerNames = pDeviceLayers;
				logicalDeviceCreateInfo.enabledExtensionCount = (uint)deviceExtensions.Length;
				logicalDeviceCreateInfo.ppEnabledExtensionNames = pDeviceExtensions;
				logicalDeviceCreateInfo.pEnabledFeatures = &deviceFeatures;

				if (Vulkan.VulkanNative.vkCreateDevice(physicalDevice, &logicalDeviceCreateInfo, null, pDevice) != Vulkan.RawConstants.VK_SUCCESS)
				{
					throw new SystemException();
				}
			}

			/* Get queue handles */
			fixed (Vulkan.VkQueue* pGraphicsQueue = &graphicsQueue)
			fixed (Vulkan.VkQueue* pPresentQueue = &presentQueue)
			fixed (Vulkan.VkQueue* pTransferQueue = &transferQueue)
			{
				Vulkan.VulkanNative.vkGetDeviceQueue(device, (uint)queueFamilies.graphicsFamily, 0, pGraphicsQueue);
				Vulkan.VulkanNative.vkGetDeviceQueue(device, (uint)queueFamilies.presentFamily, 0, pPresentQueue);
				Vulkan.VulkanNative.vkGetDeviceQueue(device, (uint)queueFamilies.transferFamily, 0, pTransferQueue);
			}

			/* Create allocator */
			memoryAllocator = new VulkanMemoryAllocator(physicalDevice, device);

			/* Create command pools */
			Vulkan.VkCommandPoolCreateInfo graphicsPoolInfo = new Vulkan.VkCommandPoolCreateInfo();
			graphicsPoolInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
			graphicsPoolInfo.flags = Vulkan.RawConstants.VK_COMMAND_POOL_CREATE_TRANSIENT_BIT | Vulkan.RawConstants.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
			graphicsPoolInfo.queueFamilyIndex = (uint)queueFamilies.graphicsFamily;

			fixed(Vulkan.VkCommandPool* pGraphicsCommandPool = &graphicsCommandPool)
			{
				if (Vulkan.VulkanNative.vkCreateCommandPool(device, &graphicsPoolInfo, null, pGraphicsCommandPool) != Vulkan.RawConstants.VK_SUCCESS)
				{
					throw new SystemException();
				}
			}

			// Even though the graphics queue and transfer queue might be the same, and therefore we could
			// use the graphics command pool, we'll make a new one so that it can be optimized for transfer
			// operations
			Vulkan.VkCommandPoolCreateInfo transferPoolInfo = new Vulkan.VkCommandPoolCreateInfo();
			transferPoolInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
			transferPoolInfo.flags = Vulkan.RawConstants.VK_COMMAND_POOL_CREATE_TRANSIENT_BIT;
			transferPoolInfo.queueFamilyIndex = (uint)queueFamilies.transferFamily;

			fixed (Vulkan.VkCommandPool* pTransferCommandPool = &transferCommandPool)
			{
				if (Vulkan.VulkanNative.vkCreateCommandPool(device, &transferPoolInfo, null, pTransferCommandPool) != Vulkan.RawConstants.VK_SUCCESS)
				{
					throw new SystemException();
				}
			}
		}

		private unsafe void createSwapChain()
		{
			/* Get swap chain details */
			SwapChainSupportDetails swapChainSupport = querySwapChainSupport();
			VkSurfaceFormatKHR surfaceFormat = chooseSwapSurfaceFormat(swapChainSupport.formats);
			VkPresentModeKHR presentMode = chooseSwapPresentMode(swapChainSupport.presentModes);
			VkExtent2D extent = chooseSwapExtent(windowWidth, windowHeight, swapChainSupport.capabilities);

			/* Determine how many images will be in swap chain */
			// try to have minImageCount + 1 (for tripple buffering), otherwise maxImageCount
			uint imageCount = Math.Min(swapChainSupport.capabilities.minImageCount + 1, (swapChainSupport.capabilities.maxImageCount > 0) ? swapChainSupport.capabilities.minImageCount : UInt32.MaxValue);
			Console.WriteLine("Min swapchain images: {0}", imageCount);

			/* Create swap chain */
			//VkSwapchainKHR oldSwapchain = new VkSwapchainKHR(swapchain.Handle);
			VkSwapchainCreateInfoKHR swapChainCreateInfo = new Vulkan.VkSwapchainCreateInfoKHR();
			swapChainCreateInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR;
			swapChainCreateInfo.surface = surface;
			swapChainCreateInfo.minImageCount = imageCount;
			swapChainCreateInfo.imageFormat = surfaceFormat.format;
			swapChainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
			swapChainCreateInfo.imageExtent = extent;
			swapChainCreateInfo.imageArrayLayers = 1;
			swapChainCreateInfo.imageUsage = Vulkan.RawConstants.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
			swapChainCreateInfo.preTransform = swapChainSupport.capabilities.currentTransform;
			swapChainCreateInfo.compositeAlpha = Vulkan.RawConstants.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
			swapChainCreateInfo.presentMode = presentMode;
			swapChainCreateInfo.clipped = Vulkan.RawConstants.VK_TRUE;
			swapChainCreateInfo.oldSwapchain = VkSwapchainKHR.Null;

			// if our queues are not using the same index, shareing needs to be concurrent with each unique queue family
			uint[] queueIndices = { graphicsQueueIndex, transferQueueIndex };
			fixed (uint* pQueueIndices = queueIndices)
			fixed (VkSwapchainKHR* pSwapchain = &swapchain)
			{
				if (graphicsQueueIndex != transferQueueIndex)
				{
					swapChainCreateInfo.imageSharingMode = Vulkan.RawConstants.VK_SHARING_MODE_CONCURRENT;
					swapChainCreateInfo.queueFamilyIndexCount = (uint)queueIndices.Length;
					swapChainCreateInfo.pQueueFamilyIndices = pQueueIndices;
				}
				else
				{
					swapChainCreateInfo.imageSharingMode = Vulkan.RawConstants.VK_SHARING_MODE_EXCLUSIVE;
					swapChainCreateInfo.queueFamilyIndexCount = 0; // Optional
					swapChainCreateInfo.pQueueFamilyIndices = null; // Optional
				}

				// create swap chain
				if (Vulkan.VulkanNative.vkCreateSwapchainKHR(device, &swapChainCreateInfo, null, pSwapchain) != Vulkan.RawConstants.VK_SUCCESS)
				{
					throw new SystemException();
				}
				// if oldSwapchain was not null, delete it now
				/*
				if (oldSwapchain.Handle != 0)
				{
					// make sure device is idle (old swap chain has finished processing)
					Vulkan.VulkanNative.vkDeviceWaitIdle(device);
					Vulkan.VulkanNative.vkDestroySwapchainKHR(device, oldSwapchain, null);
				}
				*/
			}

			// remember swapchain format and extent
			swapchainImageFormat = surfaceFormat.format;
			swapchainImageExtent = extent;

			/* Get swap chain images */
			Vulkan.VulkanNative.vkGetSwapchainImagesKHR(device, swapchain, &imageCount, null);
			swapchainImages = new Vulkan.VkImage[imageCount];
			fixed (Vulkan.VkImage* pSwapchainImages = swapchainImages)
			{
				Vulkan.VulkanNative.vkGetSwapchainImagesKHR(device, swapchain, &imageCount, pSwapchainImages);
			}

			Console.WriteLine("Swap chain created with {0} images\n", imageCount);

			/* Create swap chain image views */
			swapchainImageViews = new Vulkan.VkImageView[imageCount];
			for (uint index = 0; index < imageCount; index++)
			{
				VkImage image = swapchainImages[index];
				VkImageViewCreateInfo imageViewCreateInfo = new Vulkan.VkImageViewCreateInfo();
				imageViewCreateInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
				imageViewCreateInfo.image = image;
				imageViewCreateInfo.viewType = Vulkan.RawConstants.VK_IMAGE_VIEW_TYPE_2D;
				imageViewCreateInfo.format = surfaceFormat.format;
				imageViewCreateInfo.components = new Vulkan.VkComponentMapping();
				imageViewCreateInfo.components.r = Vulkan.RawConstants.VK_COMPONENT_SWIZZLE_R;
				imageViewCreateInfo.components.g = Vulkan.RawConstants.VK_COMPONENT_SWIZZLE_G;
				imageViewCreateInfo.components.b = Vulkan.RawConstants.VK_COMPONENT_SWIZZLE_B;
				imageViewCreateInfo.components.a = Vulkan.RawConstants.VK_COMPONENT_SWIZZLE_A;
				imageViewCreateInfo.subresourceRange = new Vulkan.VkImageSubresourceRange();
				imageViewCreateInfo.subresourceRange.aspectMask = Vulkan.RawConstants.VK_IMAGE_ASPECT_COLOR_BIT;
				imageViewCreateInfo.subresourceRange.baseMipLevel = 0;
				imageViewCreateInfo.subresourceRange.levelCount = 1;
				imageViewCreateInfo.subresourceRange.baseArrayLayer = 0;
				imageViewCreateInfo.subresourceRange.layerCount = 1;

				fixed (Vulkan.VkImageView* pImageView =  &swapchainImageViews[index])
				{
					if (Vulkan.VulkanNative.vkCreateImageView(device, &imageViewCreateInfo, null, pImageView) != Vulkan.RawConstants.VK_SUCCESS)
					{
						throw new SystemException();
					}
				}

				// transition image - renderer expects image to be in present layout
				transitionImageLayout(
						device, graphicsCommandPool, graphicsQueue,
						swapchainImages[index], swapchainImageFormat,
					Vulkan.RawConstants.VK_IMAGE_LAYOUT_UNDEFINED, Vulkan.RawConstants.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);
			}

			/* Create depth buffer */
			// choose depth buffer format
			VkFormat[] depthImageFormatOptions = { Vulkan.RawConstants.VK_FORMAT_D32_SFLOAT, Vulkan.RawConstants.VK_FORMAT_D32_SFLOAT_S8_UINT, Vulkan.RawConstants.VK_FORMAT_D24_UNORM_S8_UINT};
			depthImageFormat = findSupportedFormat(
				depthImageFormatOptions,
                physicalDevice,
				Vulkan.RawConstants.VK_IMAGE_TILING_OPTIMAL,
				Vulkan.RawConstants.VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
            );

			HashSet<uint> queueIndexSet = new HashSet<uint>();
			queueIndexSet.Add((uint)queueFamilies.graphicsFamily);
			queueIndexSet.Add((uint)queueFamilies.transferFamily);
			queueIndexSet.Add((uint)queueFamilies.presentFamily);
			uint[] uniqueQueueIndices = new uint[queueIndexSet.Count];
			int i = 0;
			foreach (uint index in queueIndexSet)
			{
				uniqueQueueIndices[i] = index;
				i++;
			}

			fixed (VkImage* pDepthImage = &depthImage)
			fixed (VmaAllocation* pDepthImageAllocation = &depthImageAllocation)
			{
				createImage2D(
						memoryAllocator,
						uniqueQueueIndices,
						swapchainImageExtent.width,
						swapchainImageExtent.height,
						depthImageFormat,
						RawConstants.VK_IMAGE_TILING_OPTIMAL,
						RawConstants.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT,
						RawConstants.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
						0,
						pDepthImage,
						pDepthImageAllocation
					);
			}

			// create image view
			VkImageViewCreateInfo viewInfo = new VkImageViewCreateInfo();
			viewInfo.sType = RawConstants.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
			viewInfo.image = depthImage;
			viewInfo.viewType = RawConstants.VK_IMAGE_VIEW_TYPE_2D;
			viewInfo.format = depthImageFormat;
			viewInfo.subresourceRange = new VkImageSubresourceRange();
			viewInfo.subresourceRange.aspectMask = RawConstants.VK_IMAGE_ASPECT_DEPTH_BIT;
			if (hasStencilComponent(depthImageFormat))
			{
				viewInfo.subresourceRange.aspectMask |= RawConstants.VK_IMAGE_ASPECT_STENCIL_BIT;
			}
			viewInfo.subresourceRange.baseMipLevel = 0;
			viewInfo.subresourceRange.levelCount = 1;
			viewInfo.subresourceRange.baseArrayLayer = 0;
			viewInfo.subresourceRange.layerCount = 1;

			fixed (VkImageView* pDepthImageView = &depthImageView)
			{
				if (VulkanNative.vkCreateImageView(device, &viewInfo, null, pDepthImageView) != RawConstants.VK_SUCCESS)
				{
					throw new Exception();
				}
			}

			// transition depth buffer
			transitionImageLayout(device, graphicsCommandPool, graphicsQueue, depthImage, depthImageFormat, 
			                      RawConstants.VK_IMAGE_LAYOUT_UNDEFINED, RawConstants.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL);

			/* Create command buffers */
			VkCommandBufferAllocateInfo allocInfo = new VkCommandBufferAllocateInfo();
			allocInfo.sType = RawConstants.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
			allocInfo.commandPool = graphicsCommandPool;
			allocInfo.level = RawConstants.VK_COMMAND_BUFFER_LEVEL_PRIMARY;
			allocInfo.commandBufferCount = imageCount;

			swapchainCommandBuffers = new VkCommandBuffer[imageCount];
			fixed (VkCommandBuffer* pSwapchainCommandBuffers = swapchainCommandBuffers)
			{
				if (VulkanNative.vkAllocateCommandBuffers(device, &allocInfo, pSwapchainCommandBuffers) != RawConstants.VK_SUCCESS)
				{
					throw new Exception();
				}
			}

			/* Create semaphores and fences */
			VkSemaphoreCreateInfo semaphoreInfo = new VkSemaphoreCreateInfo();
			semaphoreInfo.sType = RawConstants.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO;
			VkFenceCreateInfo fenceInfo = new VkFenceCreateInfo();
			fenceInfo.sType = RawConstants.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
			fenceInfo.flags = RawConstants.VK_FENCE_CREATE_SIGNALED_BIT;

			// make imageCount - 1 sync objects; all frames not being presented can be 'in flight'
			imageAvailableSemaphores = new VkSemaphore[imageCount];
			renderFinishedSemaphores = new VkSemaphore[imageCount];
			inFlightFences = new VkFence[imageCount];
			for (ulong j = 0; j < imageCount; j++)
			{
				fixed (VkSemaphore* pImageAvailableSemaphore = &imageAvailableSemaphores[i])
				fixed (VkSemaphore* pRenderFinishedSemaphore = &renderFinishedSemaphores[i])
				fixed (VkFence* pInFlightFence = &inFlightFences[i])
				{
					if (VulkanNative.vkCreateSemaphore(device, &semaphoreInfo, null, pImageAvailableSemaphore) != RawConstants.VK_SUCCESS ||
						VulkanNative.vkCreateSemaphore(device, &semaphoreInfo, null, pRenderFinishedSemaphore) != RawConstants.VK_SUCCESS ||
						VulkanNative.vkCreateFence(device, &fenceInfo, null, pInFlightFence) != RawConstants.VK_SUCCESS)
					{
						throw new Exception();
					}
				}
			}

			/* Push changes to any pipelines */
			/*
			for (std::pair<size_t, VulkanPipeline*> pipeline : pipelines)
			{
				pipeline.second->updateResources(swapchainImageFormat, depthImageFormat, imageCount, swapchainImageViews, depthImageView);
			}
			*/

			/* Initialize the images for the render and present targets */
			/*
			// no image should be presented right now
			presentTarget.renderSubmitted = false;

			// get image to render
			if (vkAcquireNextImageKHR(
					logicalDevice, swapchain, std::numeric_limits < uint32_t >::max(),
					imageAvailableSemaphores[currentFrame], VK_NULL_HANDLE, &renderTarget.frameIndex) != VK_SUCCESS)
			{
				throw Error("Unable to get image from swapchain");
			}
			renderTarget.imageAvailableSemaphore = imageAvailableSemaphores[renderTarget.frameIndex];
			renderTarget.renderFinishedSemaphore = renderFinishedSemaphores[renderTarget.frameIndex];
			// reset the fence for the first image to be rendered
			vkResetFences(logicalDevice, 1, &inFlightFences[renderTarget.frameIndex]);
			*/
		}

		private unsafe SwapChainSupportDetails querySwapChainSupport()
		{
			SwapChainSupportDetails details = new SwapChainSupportDetails();
			uint formatCount;
			uint presentModeCount;

			Vulkan.VulkanNative.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, &details.capabilities);

			Vulkan.VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &formatCount, null);
			if (formatCount != 0)
			{
				details.formats = new Vulkan.VkSurfaceFormatKHR[formatCount];
				fixed (Vulkan.VkSurfaceFormatKHR* pFormats = details.formats)
				{
					Vulkan.VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, &formatCount, pFormats);
				}
			}

			Vulkan.VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, null);
			if (presentModeCount != 0)
			{
				details.presentModes = new Vulkan.VkPresentModeKHR[presentModeCount];
				fixed (Vulkan.VkPresentModeKHR* pPresentModes = details.presentModes)
				{
					Vulkan.VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, pPresentModes);
				}
			}
			return details;
		}

		private static Vulkan.VkSurfaceFormatKHR chooseSwapSurfaceFormat(Vulkan.VkSurfaceFormatKHR[] availableFormats)
		{
			/* If surface has no prefered formats, chose a default */
			if (availableFormats.Length == 1 && availableFormats[0].format == Vulkan.RawConstants.VK_FORMAT_UNDEFINED)
			{
				Console.WriteLine("Using swapchain surface format: sRGB B8G8R8A8_UNORM");
				Vulkan.VkSurfaceFormatKHR format = new Vulkan.VkSurfaceFormatKHR();
				format.format = Vulkan.RawConstants.VK_FORMAT_B8G8R8A8_UNORM;
				format.colorSpace = Vulkan.RawConstants.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR;
				return format;
			}

			/* Otherwise, look for prefered combination */
			foreach (Vulkan.VkSurfaceFormatKHR format in availableFormats){
				if (format.format == Vulkan.RawConstants.VK_FORMAT_B8G8R8A8_UNORM && format.colorSpace == Vulkan.RawConstants.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
				{
					Console.WriteLine("Using swapchain surface format: sRGB B8G8R8A8_UNORM");
					return format;
				}
			}

			/* If no prefered combination is found, just use the first available format */
			Console.WriteLine("Using swapchain format: {0}, in colorspace {1}", availableFormats[0].format, availableFormats[0].colorSpace);
			return availableFormats[0];
		}

		private static Vulkan.VkPresentModeKHR chooseSwapPresentMode(Vulkan.VkPresentModeKHR[] availablePresentModes)
		{
			/* look for 'mailbox' present mode, for tripple buffering */
			foreach (Vulkan.VkPresentModeKHR mode in availablePresentModes){
				if (mode == Vulkan.RawConstants.VK_PRESENT_MODE_MAILBOX_KHR)
				{
					Console.WriteLine("Using present mode: Mailbox");
					return mode;
				}
			}

			/* otherwise, prefer immediate (some drivers don't properly implement fifo) */
			foreach (Vulkan.VkPresentModeKHR mode in availablePresentModes){
				if (mode == Vulkan.RawConstants.VK_PRESENT_MODE_IMMEDIATE_KHR)
				{
					Console.WriteLine("Using present mode: Immediate");
					return mode;
				}
			}

			/* fifo is always guaranteed to be available, but isn't always well supported, so use it as a last resort */
			Console.WriteLine("Using present mode: FIFO\n");
			return Vulkan.RawConstants.VK_PRESENT_MODE_FIFO_KHR;
		}

		private static Vulkan.VkExtent2D chooseSwapExtent(int preferedWidth, int preferedHeight, Vulkan.VkSurfaceCapabilitiesKHR surfaceCapabilities)
		{
			/* if currentExtent has dimentions of the maximum value of uint32_t, that's Vulkan's signal that
     		* the extent can be changed. We'll set it to the dimentions of the window. Otherwise use the 
     		* extent Vulkan gives us. */
			if (surfaceCapabilities.currentExtent.width != UInt32.MaxValue)
			{
				Console.WriteLine("Using swap chain extent: ({0}, {1})", surfaceCapabilities.currentExtent.width, surfaceCapabilities.currentExtent.height);
				return surfaceCapabilities.currentExtent;
			}
			else
			{
				VkExtent2D extent = new Vulkan.VkExtent2D();
				extent.width = (uint)preferedWidth;
				extent.height = (uint)preferedHeight;

				// get closest dimentions to the window's size within the min and max extents given
				extent.width = Math.Max(surfaceCapabilities.minImageExtent.width, Math.Max(surfaceCapabilities.maxImageExtent.width, extent.width));
				extent.width = Math.Max(surfaceCapabilities.minImageExtent.height, Math.Max(surfaceCapabilities.maxImageExtent.height, extent.height));

				Console.WriteLine("Using swap chain extent: ({0}, {1})", extent.width, extent.height);
				return extent;
			}
		}

		private unsafe static void transitionImageLayout(VkDevice device, VkCommandPool pool, VkQueue queue, VkImage image,
												 VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
		{
			VkCommandBuffer commandBuffer = beginSingleTimeCommands(device, pool);

			VkImageMemoryBarrier barrier = new VkImageMemoryBarrier();
			barrier.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;
			barrier.oldLayout = oldLayout;
			barrier.newLayout = newLayout;
			barrier.srcQueueFamilyIndex = Vulkan.RawConstants.VK_QUEUE_FAMILY_IGNORED;
			barrier.dstQueueFamilyIndex = Vulkan.RawConstants.VK_QUEUE_FAMILY_IGNORED;
			barrier.image = image;
			barrier.subresourceRange = new VkImageSubresourceRange();
			barrier.subresourceRange.baseMipLevel = 0;
			barrier.subresourceRange.levelCount = 1;
			barrier.subresourceRange.baseArrayLayer = 0;
			barrier.subresourceRange.layerCount = 1;

			// if we're transitioning a depth/stencil image, change some settings
			if (newLayout == Vulkan.RawConstants.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL)
			{
				barrier.subresourceRange.aspectMask = Vulkan.RawConstants.VK_IMAGE_ASPECT_DEPTH_BIT;

				if (hasStencilComponent(format))
				{
					barrier.subresourceRange.aspectMask |= Vulkan.RawConstants.VK_IMAGE_ASPECT_STENCIL_BIT;
				}
			}
			else
			{
				barrier.subresourceRange.aspectMask = Vulkan.RawConstants.VK_IMAGE_ASPECT_COLOR_BIT;
			}

			VkPipelineStageFlags sourceStage;
			VkPipelineStageFlags destinationStage;

			switch (oldLayout)
			{
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_PREINITIALIZED:
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_UNDEFINED:
					/* Because the layout is undefined, it is not being used for anything before the
					 * transition, therefore we don't need to wait on any stage. Tell the driver to
					 * start the transition as soon as possible, giving it the freedom to choose the
					 * best place to do the transition.
					 */
					sourceStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
					barrier.srcAccessMask = 0;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
					/* The image is being used as a transfer destination, so don't start the transition
					 * until after the transfer write stage
					 */
					sourceStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_TRANSFER_BIT;
					barrier.srcAccessMask = Vulkan.RawConstants.VK_ACCESS_TRANSFER_WRITE_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR:
					sourceStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
					barrier.srcAccessMask = Vulkan.RawConstants.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
					sourceStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
					barrier.srcAccessMask = Vulkan.RawConstants.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
					break;
				default:
					Console.WriteLine("Image transition from unsupported layout ({0}) is not optimal", oldLayout);
					goto case Vulkan.RawConstants.VK_IMAGE_LAYOUT_GENERAL; // C# doesn't allow fallthrough if case has any code
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_GENERAL:
					/* We don't know what the image was used for before, so to be safe don't start the
					 * transition until the end of the pipe
					 */
					sourceStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
					barrier.srcAccessMask = 0;
					break;
			}

			switch (newLayout)
			{
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_PREINITIALIZED:
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_UNDEFINED:
					// transitioning image to preinitalized or undefined is not allowed
					throw new InvalidOperationException();
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
					/* The image is being used as a transfer destination, so make sure the transition
					 * is done before that stage
					 */
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_TRANSFER_BIT;
					barrier.dstAccessMask = Vulkan.RawConstants.VK_ACCESS_TRANSFER_WRITE_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:
					/* Image will be read by a shader, so make sure transition is done before then
					 */
					// technically, the image may not be consumed by the vertex shader, and we could do the
					// transition later, but we have no way of telling what shader this image will be for
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_VERTEX_SHADER_BIT;
					barrier.dstAccessMask = Vulkan.RawConstants.VK_ACCESS_SHADER_READ_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL:
					/* Image will be used for depth tests, so make sure it's transitioned before the
					 * fragment test stage
					 */
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;
					barrier.dstAccessMask = Vulkan.RawConstants.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT | RawConstants.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
					barrier.dstAccessMask = Vulkan.RawConstants.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
					break;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR:
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
					barrier.dstAccessMask = Vulkan.RawConstants.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT;
					break;
				default:
					Console.WriteLine("Image transition to unsupported layout ({0}) is not optimal", newLayout);
					goto case Vulkan.RawConstants.VK_IMAGE_LAYOUT_GENERAL;
				case Vulkan.RawConstants.VK_IMAGE_LAYOUT_GENERAL:
					/* We don't know what the image will be used for, so make sure the transition is done
					 * by the start of the pipe
					 */
					destinationStage = Vulkan.RawConstants.VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
					barrier.dstAccessMask = 0;
					break;
			}

			Vulkan.VulkanNative.vkCmdPipelineBarrier(
					commandBuffer,
					sourceStage, destinationStage,
					0,
					0, null,
					0, null,
					1, &barrier);

			endSingleTimeCommands(device, commandBuffer, pool, queue);
			Vulkan.VulkanNative.vkDeviceWaitIdle(device);
		}

		private static unsafe VkCommandBuffer beginSingleTimeCommands(VkDevice device, VkCommandPool pool)
		{
			VkCommandBufferAllocateInfo allocInfo = new VkCommandBufferAllocateInfo();
			allocInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
			allocInfo.level = Vulkan.RawConstants.VK_COMMAND_BUFFER_LEVEL_PRIMARY;
			allocInfo.commandPool = pool;
			allocInfo.commandBufferCount = 1;

			VkCommandBuffer commandBuffer = new VkCommandBuffer();
			Vulkan.VulkanNative.vkAllocateCommandBuffers(device, &allocInfo, &commandBuffer);

			VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo();
			beginInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
			beginInfo.flags = Vulkan.RawConstants.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;

			Vulkan.VulkanNative.vkBeginCommandBuffer(commandBuffer, &beginInfo);

			return commandBuffer;
		}

		private static unsafe void endSingleTimeCommands(VkDevice device, VkCommandBuffer commandBuffer, VkCommandPool pool, VkQueue submitQueue)
		{
			/* Submit command buffer */
			Vulkan.VulkanNative.vkEndCommandBuffer(commandBuffer);

			// create fence
			VkFence transferDone;
			VkFenceCreateInfo fenceInfo = new VkFenceCreateInfo();
			fenceInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;

			if (Vulkan.VulkanNative.vkCreateFence(device, &fenceInfo, null, &transferDone) != Vulkan.RawConstants.VK_SUCCESS)
			{
				throw new SystemException();
			}

			// submit
			VkSubmitInfo submitInfo = new VkSubmitInfo();
			submitInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_SUBMIT_INFO;
			submitInfo.commandBufferCount = 1;
			submitInfo.pCommandBuffers = &commandBuffer;

			Vulkan.VulkanNative.vkQueueSubmit(submitQueue, 1, &submitInfo, transferDone);

			/* Wait for command buffer to finish */
			Vulkan.VulkanNative.vkWaitForFences(device, 1, &transferDone, Vulkan.RawConstants.VK_TRUE, UInt64.MaxValue);

			/* Clean up */
			Vulkan.VulkanNative.vkDestroyFence(device, transferDone, null);
			Vulkan.VulkanNative.vkFreeCommandBuffers(device, pool, 1, &commandBuffer);
		}

		private static bool hasStencilComponent(VkFormat format)
		{
			return format == Vulkan.RawConstants.VK_FORMAT_D32_SFLOAT_S8_UINT || format == Vulkan.RawConstants.VK_FORMAT_D24_UNORM_S8_UINT;
		}

		private static unsafe VkFormat findSupportedFormat(VkFormat[] candidates, VkPhysicalDevice physicalDevice, VkImageTiling tiling, VkFormatFeatureFlags features) {
		    foreach (VkFormat format in candidates){
				VkFormatProperties properties;

				Vulkan.VulkanNative.vkGetPhysicalDeviceFormatProperties(physicalDevice, format, &properties);
				if (tiling == Vulkan.RawConstants.VK_IMAGE_TILING_LINEAR && (properties.linearTilingFeatures & features) == features) {
		            return format;
				} else if (tiling == Vulkan.RawConstants.VK_IMAGE_TILING_OPTIMAL && (properties.optimalTilingFeatures & features) == features) {
		            return format;
		        }
		    }
			throw new Exception();
		}

		private static unsafe void createImage2D(
			VulkanMemoryAllocator memoryAllocator,
			uint[] uniqueQueueIndices,
	        uint width,
			uint height,
	        VkFormat format,
			VkImageTiling tiling,
	        VkImageUsageFlags usage,
			VkMemoryPropertyFlags requiredFlags,
	        VkMemoryPropertyFlags preferredFlags,
			VkImage* pImage,
			VmaAllocation* pAllocation)
		{
			fixed (uint* pQueueFamilyIndices = uniqueQueueIndices)
			{
				VkImageCreateInfo imageInfo = new VkImageCreateInfo();
				imageInfo.sType = Vulkan.RawConstants.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO;
				imageInfo.flags = 0;
				imageInfo.imageType = Vulkan.RawConstants.VK_IMAGE_TYPE_2D;
				imageInfo.format = format;
				imageInfo.extent = new VkExtent3D();
				imageInfo.extent.width = (uint)width;
				imageInfo.extent.height = (uint)height;
				imageInfo.extent.depth = 1;
				imageInfo.mipLevels = 1;
				imageInfo.arrayLayers = 1;
				imageInfo.samples = Vulkan.RawConstants.VK_SAMPLE_COUNT_1_BIT;
				imageInfo.tiling = tiling;
				imageInfo.usage = usage;
				imageInfo.initialLayout = Vulkan.RawConstants.VK_IMAGE_LAYOUT_UNDEFINED;

				if (uniqueQueueIndices.Length > 1)
				{
					imageInfo.sharingMode = Vulkan.RawConstants.VK_SHARING_MODE_CONCURRENT;
					imageInfo.queueFamilyIndexCount = (uint)uniqueQueueIndices.Length;
					imageInfo.pQueueFamilyIndices = pQueueFamilyIndices;
				}
				else
				{
					imageInfo.sharingMode = Vulkan.RawConstants.VK_SHARING_MODE_EXCLUSIVE;
				}

				VmaAllocationCreateInfo allocInfo = new VmaAllocationCreateInfo();
				allocInfo.usage = VmaMemoryUsage.VMA_MEMORY_USAGE_UNKNOWN;
				allocInfo.requiredFlags = requiredFlags;
				allocInfo.preferredFlags = preferredFlags;

				VkResult result = memoryAllocator.CreateImage(&imageInfo, &allocInfo, pImage, pAllocation);
				if (result != RawConstants.VK_SUCCESS)
				{
					throw new Exception();
				}
			}
		}

		// Starts recording the buffer that will be submitted when it's time to render, and returns that buffer for use by the caller
		// Finalize recording with FinalizeRenderCommandBuffer()
		// Synchronized so that we don't overwrite the command buffer while it's being used
		public Vulkan.VkCommandBuffer RecordRenderCommandBuffer()
		{
			return Vulkan.VkCommandBuffer.Null;
		}
		public void FinalizeRenderCommandBuffer()
		{
		}

		private unsafe static uint DebugReportCallback(uint flags, Vulkan.VkDebugReportObjectTypeEXT objectType, ulong @object, UIntPtr location, int messageCode, byte* layerPrefix, byte* message, void* userData)
		{
			Console.WriteLine("debug callback called");
			string layerString = new string((char*)layerPrefix);
			string messageString = new string((char*)message);

			if (((Vulkan.VkDebugReportFlagsEXT)flags).HasFlag(Vulkan.VkDebugReportFlagsEXT.ErrorEXT))
			{
				System.Console.WriteLine("[error] Vulkan validation layer: {0}", messageString);
			}
			else if (((Vulkan.VkDebugReportFlagsEXT)flags).HasFlag(Vulkan.VkDebugReportFlagsEXT.WarningEXT) | ((Vulkan.VkDebugReportFlagsEXT)flags).HasFlag(Vulkan.VkDebugReportFlagsEXT.PerformanceWarningEXT))
			{
				System.Console.WriteLine("[warn] Vulkan validation layer: {0}", messageString);
			}
			else if (((Vulkan.VkDebugReportFlagsEXT)flags).HasFlag(Vulkan.VkDebugReportFlagsEXT.DebugEXT))
			{
				// System.Console.WriteLine("[debug] Vulkan validation layer: {0}", messageString);
			}
			else if (((Vulkan.VkDebugReportFlagsEXT)flags).HasFlag(Vulkan.VkDebugReportFlagsEXT.InformationEXT))
			{
				// System.Console.WriteLine("[info] Vulkan validation layer: {0}", messageString);
			}

			return 0;
		}
	}
}
