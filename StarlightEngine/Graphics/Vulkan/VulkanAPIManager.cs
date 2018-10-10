using System;
using System.Collections.Generic;
using System.Threading;
using VulkanCore;
using VulkanCore.Khr;
using VulkanCore.Ext;
using StarlightEngine.Graphics.Vulkan.Memory;
using StarlightEngine.Threadding;

namespace StarlightEngine.Graphics.Vulkan
{
    public class VulkanAPIManager : IGraphicsAPIManager
    {
        private readonly string[] requiredInstanceExtensions = { };
        private readonly string[] preferredInstanceExtensions = {
			#if DEBUG
			Constant.InstanceExtension.ExtDebugReport
			#endif
		};
        private readonly string[] requiredInstanceLayers = { };
        private readonly string[] preferredInstanceLayers = {
			#if DEBUG
			Constant.InstanceLayer.LunarGStandardValidation
			#endif
		};
        private readonly string[] requiredDeviceExtensions = {
            "VK_KHR_swapchain"
        };
        private readonly string[] preferredDeviceExtensions = { };

        private IWindowManager m_window;
        private Instance m_instance;
        private SurfaceKhr m_surface;
        private DebugReportCallbackExt m_debugReportCallback;

        private PhysicalDevice m_physicalDevice;
        private DeviceQueueFamilies m_deviceQueueFamilies;
        private Device m_device;
        private VulkanMemoryAllocator m_memoryAllocator;
        private Queue m_graphicsQueue;
        private Queue m_transferQueue;
        private Queue m_presentQueue;

        Dictionary<IntPtr, ThreadLock> m_queueLocks = new Dictionary<IntPtr, ThreadLock>();

        private CommandPool m_graphicsCommandPool;
        private CommandPool m_transferCommandPool;

        Dictionary<long, ThreadLock> m_poolLocks = new Dictionary<long, ThreadLock>();

        // Swapchain info
        private SwapchainKhr m_swapchain;
        private Format m_swapchainImageFormat;
        private Extent2D m_swapchainImageExtent;
        private Image[] m_swapchainImages;
        private ImageView[] m_swapchainImageViews;
        private Format m_depthImageFormat;
        private Image m_depthImage;
        private VmaAllocation m_depthImageAllocation;
        private ImageView m_depthImageView;
        private CommandBuffer[] m_swapchainCommandBuffers;
        private VulkanCore.Semaphore[] m_imageAvailableSemaphores;
        private VulkanCore.Semaphore[] m_renderFinishedSemaphores;
        private Fence[] m_inFlightFences;
        private bool[] m_frameInFlight;
        private ThreadLock[] m_inFlightFenceLocks;
        private ThreadLock[] m_swapchainBufferLocks;
        private int m_currentFrame;
        private int m_frameIndex;

        public VulkanAPIManager(IWindowManager window)
        {
            m_window = window;

            List<string> requiredExtensions = new List<string>();
            requiredExtensions.AddRange(requiredInstanceExtensions);
            requiredExtensions.AddRange(window.GetVulkanExtensions());
            requiredInstanceExtensions = requiredExtensions.ToArray();

            CreateInstance(requiredInstanceExtensions, preferredInstanceExtensions, requiredInstanceLayers, preferredInstanceLayers);

            m_surface = window.GetVulkanSurface(m_instance);

            ChoosePhysicalDevice();
            CreateLogicalDevice(requiredDeviceExtensions, preferredDeviceExtensions);

            CreateSwapchain();
        }

        ~VulkanAPIManager()
        {

        }

        private void CreateInstance(string[] requiredExtensions, string[] preferredExtensions, string[] requiredLayers, string[] preferredLayers)
        {
            /* Get available extensions and layers */
            ExtensionProperties[] availableExtensions = Instance.EnumerateExtensionProperties();
            LayerProperties[] availableLayers = Instance.EnumerateLayerProperties();

            /* Make lists of extensions and layers to load, throw an exception if a required extension or layer is not present */
            List<string> extensions = new List<string>();
            List<string> layers = new List<string>();
            foreach (string requiredExtension in requiredExtensions)
            {
                bool available = false;
                foreach (ExtensionProperties availableExtension in availableExtensions)
                {
                    if (availableExtension.ExtensionName.Equals(requiredExtension))
                    {
                        available = true;
                    }
                }
                if (!available)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    extensions.Add(requiredExtension);
                }
            }
            foreach (string preferredExtension in preferredExtensions)
            {
                bool available = false;
                foreach (ExtensionProperties availableExtension in availableExtensions)
                {
                    if (availableExtension.ExtensionName.Equals(preferredExtension))
                    {
                        available = true;
                    }
                }
                if (available)
                {
                    extensions.Add(preferredExtension);
                }
            }
            foreach (string requiredLayer in requiredLayers)
            {
                bool available = false;
                foreach (LayerProperties availableLayer in availableLayers)
                {
                    if (availableLayer.LayerName.Equals(requiredLayer))
                    {
                        available = true;
                    }
                }
                if (!available)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    layers.Add(requiredLayer);
                }
            }
            foreach (string preferredLayer in preferredLayers)
            {
                bool available = false;
                foreach (LayerProperties availableLayer in availableLayers)
                {
                    if (availableLayer.LayerName.Equals(preferredLayer))
                    {
                        available = true;
                    }
                }
                if (available)
                {
                    layers.Add(preferredLayer);
                }
            }

            /* Print out extensions and layers for debugging */
            foreach (string extension in extensions)
            {
                Console.WriteLine("Using Vulkan extension: {0}", extension);
            }
            foreach (string layer in layers)
            {
                Console.WriteLine("Using Vulkan layer: {0}", layer);
            }

            ApplicationInfo appInfo = new ApplicationInfo();
            appInfo.ApplicationName = "Vulkan Application";
            appInfo.ApplicationVersion = new VulkanCore.Version(1, 0, 0);
            appInfo.EngineName = "No Engine";
            appInfo.ApplicationVersion = new VulkanCore.Version(1, 0, 0);
            appInfo.ApiVersion = new VulkanCore.Version(1, 1, 0);

            InstanceCreateInfo instanceCreateInfo = new InstanceCreateInfo();
            instanceCreateInfo.ApplicationInfo = appInfo;
            instanceCreateInfo.EnabledExtensionNames = extensions.ToArray();
            instanceCreateInfo.EnabledLayerNames = layers.ToArray();

            m_instance = new Instance(instanceCreateInfo);
            Console.WriteLine("Vulkan Initialized!");

#if DEBUG
            DebugReportCallbackCreateInfoExt debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoExt();
            debugReportCallbackCreateInfo.Callback = DebugReportCallback;
            debugReportCallbackCreateInfo.Flags = DebugReportFlagsExt.All;

            m_debugReportCallback = m_instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo);
#endif
        }

        private static bool DebugReportCallback(DebugReportCallbackInfo reportInfo)
        {
            // For what types of reports should we print?
            DebugReportFlagsExt printFlags = DebugReportFlagsExt.Error | DebugReportFlagsExt.Warning | DebugReportFlagsExt.PerformanceWarning;
            // For what types of reports should we throw?
            DebugReportFlagsExt throwFlags = DebugReportFlagsExt.Error;

            if ((reportInfo.Flags & printFlags) != 0)
            {
                Console.WriteLine("Vulkan debug report ({0}): {1}", reportInfo.Flags.ToString(), reportInfo.Message);
            }

            if ((reportInfo.Flags & throwFlags) != 0)
            {
                throw new Exception(reportInfo.Message);
            }

            return false;
        }

        private void ChoosePhysicalDevice()
        {
            PhysicalDevice[] physicalDevices = m_instance.EnumeratePhysicalDevices();

            PhysicalDevice bestDevice = physicalDevices[0];
            int bestScore = -1;
            foreach (PhysicalDevice physicalDevice in physicalDevices)
            {
                if (!IsDeviceSuitable(physicalDevice))
                {
                    continue;
                }

                int deviceScore = ScoreDevice(physicalDevice);
                if (deviceScore > bestScore)
                {
                    bestDevice = physicalDevice;
                    bestScore = deviceScore;
                }
            }

            Console.WriteLine("Using device: {0}", bestDevice.GetProperties().DeviceName);
            m_physicalDevice = bestDevice;
            m_deviceQueueFamilies = GetDeviceQueueFamilies(m_physicalDevice);
        }

        private bool IsDeviceSuitable(PhysicalDevice physicalDevice)
        {
            return true;
        }

        private int ScoreDevice(PhysicalDevice physicalDevice)
        {
            return -1;
        }

        private DeviceQueueFamilies GetDeviceQueueFamilies(PhysicalDevice physicalDevice)
        {
            DeviceQueueFamilies queueFamilies = new DeviceQueueFamilies();

            QueueFamilyProperties[] queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();

            uint i = 0;
            foreach (QueueFamilyProperties queueFamily in queueFamilyProperties)
            {
                if (queueFamily.QueueCount > 0 && queueFamily.QueueFlags.HasFlag(VulkanCore.Queues.Graphics))
                {
                    queueFamilies.graphicsFamily = i;
                }

                if (queueFamily.QueueCount > 0 && queueFamily.QueueFlags.HasFlag(VulkanCore.Queues.Transfer))
                {
                    queueFamilies.transferFamily = i;
                }

                bool presentSupport = physicalDevice.GetSurfaceSupportKhr((int)i, m_surface);
                if (queueFamily.QueueCount > 0 && presentSupport)
                {
                    queueFamilies.presentFamily = i;
                }

                if (queueFamilies.IsComplete())
                {
                    break;
                }
                i++;
            }

            return queueFamilies;
        }

        private void CreateLogicalDevice(string[] requiredExtensions, string[] preferredExtensions)
        {
            /* Get lists of available device extensions and layers */
            ExtensionProperties[] availableDeviceExtensions = m_physicalDevice.EnumerateExtensionProperties();

            /* Create lists of extensions and layers based on what's available */
            List<string> deviceExtensions = new List<string>();
            foreach (string requiredExtension in requiredExtensions)
            {
                if (!availableDeviceExtensions.Contains(requiredExtension))
                {
                    throw new NotSupportedException();
                }
                else
                {
                    deviceExtensions.Add(requiredExtension);
                }
            }
            foreach (string preferredExtension in preferredExtensions)
            {
                if (availableDeviceExtensions.Contains(preferredExtension))
                {
                    deviceExtensions.Add(preferredExtension);
                }
            }

            DeviceQueueCreateInfo[] queueCreateInfos;
            ISet<uint> uniqueQueueFamilies = new HashSet<uint>();
            uniqueQueueFamilies.Add((uint)m_deviceQueueFamilies.graphicsFamily);
            uniqueQueueFamilies.Add((uint)m_deviceQueueFamilies.transferFamily);
            uniqueQueueFamilies.Add((uint)m_deviceQueueFamilies.presentFamily);
            queueCreateInfos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Count];

            float queuePriorityNormal = 1.0f;
            int i = 0;
            foreach (uint queueFamily in uniqueQueueFamilies)
            {
                DeviceQueueCreateInfo queueCreateInfo = new DeviceQueueCreateInfo();
                queueCreateInfo.QueueFamilyIndex = (int)queueFamily;
                queueCreateInfo.QueueCount = 1;
                queueCreateInfo.QueuePriorities = new[] { queuePriorityNormal };

                queueCreateInfos[i] = queueCreateInfo;
                i++;
            }

            PhysicalDeviceFeatures deviceFeatures = new PhysicalDeviceFeatures();
            deviceFeatures.SamplerAnisotropy = true;
            deviceFeatures.FillModeNonSolid = true;

            DeviceCreateInfo logicalDeviceCreateInfo = new DeviceCreateInfo();
            logicalDeviceCreateInfo.QueueCreateInfos = queueCreateInfos;
            logicalDeviceCreateInfo.EnabledExtensionNames = deviceExtensions.ToArray();
            logicalDeviceCreateInfo.EnabledFeatures = deviceFeatures;

            m_device = m_physicalDevice.CreateDevice(logicalDeviceCreateInfo);

            /* Get queue handles */
            m_graphicsQueue = m_device.GetQueue((int)m_deviceQueueFamilies.graphicsFamily);
            m_transferQueue = m_device.GetQueue((int)m_deviceQueueFamilies.transferFamily);
            m_presentQueue = m_device.GetQueue((int)m_deviceQueueFamilies.presentFamily);

            /* Create queue locks */
            m_queueLocks.Add(m_graphicsQueue.Handle, new ThreadLock(EngineConstants.THREADLEVEL_DIRECTAPI));
            if (!m_queueLocks.ContainsKey(m_transferQueue.Handle))
            {
                m_queueLocks.Add(m_transferQueue.Handle, new ThreadLock(EngineConstants.THREADLEVEL_DIRECTAPI));
            }
            if (!m_queueLocks.ContainsKey(m_presentQueue.Handle))
            {
                m_queueLocks.Add(m_presentQueue.Handle, new ThreadLock(EngineConstants.THREADLEVEL_DIRECTAPI));
            }

            /* Create allocator */
            m_memoryAllocator = new VulkanMemoryAllocator(m_physicalDevice, m_device);

            /* Create command pools */
            CommandPoolCreateInfo graphicsPoolInfo = new CommandPoolCreateInfo();
            graphicsPoolInfo.Flags = CommandPoolCreateFlags.Transient | CommandPoolCreateFlags.ResetCommandBuffer;
            graphicsPoolInfo.QueueFamilyIndex = (int)m_deviceQueueFamilies.graphicsFamily;

            m_graphicsCommandPool = m_device.CreateCommandPool(graphicsPoolInfo);
            m_poolLocks.Add(m_graphicsCommandPool.Handle, new ThreadLock(EngineConstants.THREADLEVEL_SWAPCHAIN_RECORD));

            // Even though the graphics queue and transfer queue might be the same, and therefore we could
            // use the graphics command pool, we'll make a new one so that it can be optimized for transfer
            // operations
            CommandPoolCreateInfo transferPoolInfo = new CommandPoolCreateInfo();
            transferPoolInfo.Flags = CommandPoolCreateFlags.Transient;
            transferPoolInfo.QueueFamilyIndex = (int)m_deviceQueueFamilies.transferFamily;

            m_transferCommandPool = m_device.CreateCommandPool(transferPoolInfo);
            m_poolLocks.Add(m_transferCommandPool.Handle, new ThreadLock(EngineConstants.THREADLEVEL_SWAPCHAIN_RECORD));
        }

        private void CreateSwapchain(bool recreate = false)
        {
            /* Get swap chain details */
            SwapChainSupportDetails swapChainSupport = querySwapChainSupport();
            SurfaceFormatKhr surfaceFormat = chooseSwapSurfaceFormat(swapChainSupport.formats);
            PresentModeKhr presentMode = chooseSwapPresentMode(swapChainSupport.presentModes);
            Extent2D extent = chooseSwapExtent(m_window.Width, m_window.Height, swapChainSupport.capabilities);

            /* Determine how many images will be in swap chain */
            // try to have minImageCount + 1 (for tripple buffering), otherwise maxImageCount
            int imageCount = System.Math.Min(swapChainSupport.capabilities.MinImageCount + 1, (swapChainSupport.capabilities.MaxImageCount > 0) ? swapChainSupport.capabilities.MinImageCount : int.MaxValue);
            Console.WriteLine("Min swapchain images: {0}", imageCount);

            /* Create swap chain */
            SwapchainCreateInfoKhr swapChainCreateInfo = new SwapchainCreateInfoKhr();
            swapChainCreateInfo.Surface = m_surface;
            swapChainCreateInfo.MinImageCount = imageCount;
            swapChainCreateInfo.ImageFormat = surfaceFormat.Format;
            swapChainCreateInfo.ImageColorSpace = surfaceFormat.ColorSpace;
            swapChainCreateInfo.ImageExtent = extent;
            swapChainCreateInfo.ImageArrayLayers = 1;
            swapChainCreateInfo.ImageUsage = ImageUsages.ColorAttachment;
            swapChainCreateInfo.PreTransform = swapChainSupport.capabilities.CurrentTransform;
            swapChainCreateInfo.CompositeAlpha = CompositeAlphasKhr.Opaque;
            swapChainCreateInfo.PresentMode = presentMode;
            swapChainCreateInfo.Clipped = true;
            swapChainCreateInfo.OldSwapchain = (recreate) ? m_swapchain : null;

            // if our queues are not using the same index, shareing needs to be concurrent with each unique queue family
            HashSet<int> queueIndicesSet = new HashSet<int>();
            queueIndicesSet.Add((int)m_deviceQueueFamilies.graphicsFamily);
            queueIndicesSet.Add((int)m_deviceQueueFamilies.transferFamily);
            int[] uniqueQueueIndices = new int[queueIndicesSet.Count];
            queueIndicesSet.CopyTo(uniqueQueueIndices);

            if (uniqueQueueIndices.Length > 1)
            {
                swapChainCreateInfo.ImageSharingMode = SharingMode.Concurrent;
                swapChainCreateInfo.QueueFamilyIndices = uniqueQueueIndices;
            }
            else
            {
                swapChainCreateInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            // create swap chain
            m_swapchain = m_device.CreateSwapchainKhr(swapChainCreateInfo);

            // remember swapchain format and extent
            m_swapchainImageFormat = surfaceFormat.Format;
            m_swapchainImageExtent = extent;

            /* Get swap chain images */
            m_swapchainImages = m_swapchain.GetImages();

            // create mutexes
            m_swapchainBufferLocks = new ThreadLock[m_swapchainImages.Length];
            for (int i = 0; i < m_swapchainImages.Length; i++)
            {
                m_swapchainBufferLocks[i] = new ThreadLock(EngineConstants.THREADLEVEL_SWAPCHAIN);
            }

            imageCount = m_swapchainImages.Length;
            Console.WriteLine("Swap chain created with {0} images", imageCount);

            /* Create swap chain image views */
            m_swapchainImageViews = new ImageView[imageCount];
            for (uint index = 0; index < imageCount; index++)
            {
                Image image = m_swapchainImages[index];
                ImageViewCreateInfo imageViewCreateInfo = new ImageViewCreateInfo();
                imageViewCreateInfo.ViewType = ImageViewType.Image2D;
                imageViewCreateInfo.Format = surfaceFormat.Format;
                imageViewCreateInfo.Components = new ComponentMapping();
                imageViewCreateInfo.Components.R = ComponentSwizzle.R;
                imageViewCreateInfo.Components.G = ComponentSwizzle.G;
                imageViewCreateInfo.Components.B = ComponentSwizzle.B;
                imageViewCreateInfo.Components.A = ComponentSwizzle.A;
                imageViewCreateInfo.SubresourceRange = new ImageSubresourceRange();
                imageViewCreateInfo.SubresourceRange.AspectMask = ImageAspects.Color;
                imageViewCreateInfo.SubresourceRange.BaseMipLevel = 0;
                imageViewCreateInfo.SubresourceRange.LevelCount = 1;
                imageViewCreateInfo.SubresourceRange.BaseArrayLayer = 0;
                imageViewCreateInfo.SubresourceRange.LayerCount = 1;

                m_swapchainImageViews[index] = image.CreateView(imageViewCreateInfo);

                // transition image - renderer expects image to be in present layout
                TransitionImageLayout(
                    image, m_swapchainImageFormat,
                    ImageLayout.Undefined, ImageLayout.PresentSrcKhr, 1);
            }

            /* Create depth buffer */
            // choose depth buffer format
            Format[] depthImageFormatOptions = { Format.D32SFloat, Format.D32SFloatS8UInt, Format.D24UNormS8UInt };
            m_depthImageFormat = FindSupportedFormat(
                depthImageFormatOptions,
                ImageTiling.Optimal,
                FormatFeatures.DepthStencilAttachment
            );

            CreateImage2D(
                m_swapchainImageExtent.Width,
                m_swapchainImageExtent.Height,
                1,
                m_depthImageFormat,
                ImageTiling.Optimal,
                ImageUsages.DepthStencilAttachment,
                MemoryProperties.DeviceLocal,
                0,
                out m_depthImage,
                out m_depthImageAllocation
                );

            // create image view
            ImageViewCreateInfo viewInfo = new ImageViewCreateInfo();
            viewInfo.ViewType = ImageViewType.Image2D;
            viewInfo.Format = m_depthImageFormat;
            viewInfo.SubresourceRange = new ImageSubresourceRange();
            viewInfo.SubresourceRange.AspectMask = ImageAspects.Depth;
            if (HasStencilComponent(m_depthImageFormat))
            {
                viewInfo.SubresourceRange.AspectMask |= ImageAspects.Stencil;
            }
            viewInfo.SubresourceRange.BaseMipLevel = 0;
            viewInfo.SubresourceRange.LevelCount = 1;
            viewInfo.SubresourceRange.BaseArrayLayer = 0;
            viewInfo.SubresourceRange.LayerCount = 1;

            m_depthImageView = m_depthImage.CreateView(viewInfo);

            // transition depth buffer
            TransitionImageLayout(m_depthImage, m_depthImageFormat,
                                  ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal, 1);

            /* Create command buffers */
            CommandBufferAllocateInfo allocInfo = new CommandBufferAllocateInfo();
            allocInfo.Level = CommandBufferLevel.Primary;
            allocInfo.CommandBufferCount = imageCount;

            m_poolLocks[m_graphicsCommandPool.Handle].EnterLock();
            m_swapchainCommandBuffers = m_graphicsCommandPool.AllocateBuffers(allocInfo);
            m_poolLocks[m_graphicsCommandPool.Handle].ExitLock();

            /* Create semaphores and fences */
            FenceCreateInfo fenceInfo = new FenceCreateInfo();
            fenceInfo.Flags = FenceCreateFlags.Signaled;

            // make imageCount - 1 sync objects; all frames not being presented can be 'in flight'
            m_imageAvailableSemaphores = new VulkanCore.Semaphore[imageCount];
            m_renderFinishedSemaphores = new VulkanCore.Semaphore[imageCount];
            m_inFlightFences = new Fence[imageCount];
            m_frameInFlight = new bool[imageCount];
            m_inFlightFenceLocks = new ThreadLock[imageCount];
            for (int j = 0; j < imageCount; j++)
            {
                m_imageAvailableSemaphores[j] = m_device.CreateSemaphore();
                m_renderFinishedSemaphores[j] = m_device.CreateSemaphore();
                m_inFlightFences[j] = m_device.CreateFence(fenceInfo);
                m_frameInFlight[j] = false;
                m_inFlightFenceLocks[j] = new ThreadLock(EngineConstants.THREADLEVEL_DIRECTAPI);
            }

            // Get first image from swapchain
            m_currentFrame = 0;
            m_frameIndex = m_swapchain.AcquireNextImage(-1, m_imageAvailableSemaphores[m_currentFrame], null);
        }

        public void GenerateMipmaps(Image image, int texWidth, int texHeight, uint mipLevels)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands(m_device, m_transferCommandPool);

            ImageMemoryBarrier barrier = new ImageMemoryBarrier();
            barrier.Image = image;
            barrier.SrcQueueFamilyIndex = Constant.QueueFamilyIgnored;
            barrier.DstQueueFamilyIndex = Constant.QueueFamilyIgnored;
            barrier.SubresourceRange.AspectMask = ImageAspects.Color;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;
            barrier.SubresourceRange.LevelCount = 1;

            int mipWidth = texWidth;
            int mipHeight = texHeight;

            for (int i = 1; i < mipLevels; i++)
            {
                // Transition source image into optimal layout (dst image is already in optimal layout)
                barrier.SubresourceRange.BaseMipLevel = i - 1;
                barrier.OldLayout = ImageLayout.TransferDstOptimal;
                barrier.NewLayout = ImageLayout.TransferSrcOptimal;
                barrier.SrcAccessMask = Accesses.TransferWrite;
                barrier.DstAccessMask = Accesses.TransferRead;

                commandBuffer.CmdPipelineBarrier(PipelineStages.Transfer, PipelineStages.Transfer, 0,
                    null,
                    null,
                    new[] { barrier });

                // Blit the image (copy 1/2 res version into next mip level)
                ImageBlit blit = new ImageBlit();
                blit.SrcOffset1.X = 0;
                blit.SrcOffset1.Y = 0;
                blit.SrcOffset1.Z = 0;
                blit.SrcOffset2.X = mipWidth;
                blit.SrcOffset2.Y = mipHeight;
                blit.SrcOffset2.Z = 1;
                blit.SrcSubresource.AspectMask = ImageAspects.Color;
                blit.SrcSubresource.MipLevel = i - 1;
                blit.SrcSubresource.BaseArrayLayer = 0;
                blit.SrcSubresource.LayerCount = 1;
                blit.DstOffset1.X = 0;
                blit.DstOffset1.Y = 0;
                blit.DstOffset1.Z = 0;
                blit.DstOffset2.X = mipWidth > 1 ? mipWidth / 2 : 1;
                blit.DstOffset2.Y = mipHeight > 1 ? mipHeight / 2 : 1;
                blit.DstOffset2.Z = 1;
                blit.DstSubresource.AspectMask = ImageAspects.Color;
                blit.DstSubresource.MipLevel = i;
                blit.DstSubresource.BaseArrayLayer = 0;
                blit.DstSubresource.LayerCount = 1;

                commandBuffer.CmdBlitImage(image, ImageLayout.TransferSrcOptimal, image, ImageLayout.TransferDstOptimal, new[] { blit }, Filter.Linear);

                // transition src image into shader read optimal
                barrier.SubresourceRange.BaseMipLevel = i - 1;
                barrier.OldLayout = ImageLayout.TransferSrcOptimal;
                barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
                barrier.SrcAccessMask = Accesses.TransferRead;
                barrier.DstAccessMask = Accesses.ShaderRead;

                commandBuffer.CmdPipelineBarrier(PipelineStages.Transfer, PipelineStages.FragmentShader, 0,
                    null,
                    null,
                    new[] { barrier });

                // divide mipWidth and mipHeight by 2 for next image
                if (mipWidth > 1) mipWidth /= 2;
                if (mipHeight > 1) mipHeight /= 2;
            }

            // transition last mip level to shader optimal
            barrier.SubresourceRange.BaseMipLevel = (int)mipLevels - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = Accesses.TransferWrite;
            barrier.DstAccessMask = Accesses.ShaderRead;

            commandBuffer.CmdPipelineBarrier(PipelineStages.Transfer, PipelineStages.FragmentShader, 0,
                null,
                null,
                new[] { barrier });

            EndSingleTimeCommands(commandBuffer, m_transferCommandPool, m_transferQueue);
        }

        private SwapChainSupportDetails querySwapChainSupport()
        {
            SwapChainSupportDetails details = new SwapChainSupportDetails();

            details.capabilities = m_physicalDevice.GetSurfaceCapabilitiesKhr(m_surface);
            details.formats = m_physicalDevice.GetSurfaceFormatsKhr(m_surface);
            details.presentModes = m_physicalDevice.GetSurfacePresentModesKhr(m_surface);

            return details;
        }

        private SurfaceFormatKhr chooseSwapSurfaceFormat(SurfaceFormatKhr[] availableFormats)
        {
            /* If surface has no prefered formats, chose a default */
            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
            {
                Console.WriteLine("Using swapchain surface format: sRGB B8G8R8A8_UNORM");
                SurfaceFormatKhr format = new SurfaceFormatKhr();
                format.Format = Format.B8G8R8A8UNorm;
                format.ColorSpace = ColorSpaceKhr.SRgbNonlinear;
                return format;
            }

            /* Otherwise, look for prefered combination */
            foreach (SurfaceFormatKhr format in availableFormats)
            {
                if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpaceKhr.SRgbNonlinear)
                {
                    Console.WriteLine("Using swapchain surface format: sRGB B8G8R8A8_UNORM");
                    return format;
                }
            }

            /* If no prefered combination is found, just use the first available format */
            Console.WriteLine("Using swapchain format: {0}, in colorspace {1}", availableFormats[0].Format, availableFormats[0].ColorSpace);
            return availableFormats[0];
        }

        private PresentModeKhr chooseSwapPresentMode(PresentModeKhr[] availablePresentModes)
        {
            /* look for 'mailbox' present mode, for tripple buffering */
            foreach (PresentModeKhr mode in availablePresentModes)
            {
                if (mode == PresentModeKhr.Mailbox)
                {
                    Console.WriteLine("Using present mode: Mailbox");
                    return mode;
                }
            }

            /* otherwise, prefer immediate (some drivers don't properly implement fifo) */
            foreach (PresentModeKhr mode in availablePresentModes)
            {
                if (mode == PresentModeKhr.Immediate)
                {
                    Console.WriteLine("Using present mode: Immediate");
                    return mode;
                }
            }

            /* fifo is always guaranteed to be available, but isn't always well supported, so use it as a last resort */
            Console.WriteLine("Using present mode: FIFO");
            return PresentModeKhr.Fifo;
        }

        private Extent2D chooseSwapExtent(int preferedWidth, int preferedHeight, SurfaceCapabilitiesKhr surfaceCapabilities)
        {
            /* if currentExtent has dimentions of the maximum value of uint32_t, that's Vulkan's signal that
     		* the extent can be changed. We'll set it to the dimentions of the window. Otherwise use the 
     		* extent Vulkan gives us. */
            if (surfaceCapabilities.CurrentExtent.Width != -1)
            {
                Console.WriteLine("Using swap chain extent: ({0}, {1})", surfaceCapabilities.CurrentExtent.Width, surfaceCapabilities.CurrentExtent.Height);
                return surfaceCapabilities.CurrentExtent;
            }
            else
            {
                Extent2D extent = new Extent2D();
                extent.Width = preferedWidth;
                extent.Height = preferedHeight;

                // get closest dimentions to the window's size within the min and max extents given
                extent.Width = System.Math.Max(surfaceCapabilities.MinImageExtent.Width, System.Math.Max(surfaceCapabilities.MaxImageExtent.Width, extent.Width));
                extent.Height = System.Math.Max(surfaceCapabilities.MinImageExtent.Height, System.Math.Max(surfaceCapabilities.MaxImageExtent.Height, extent.Height));

                Console.WriteLine("Using swap chain extent: ({0}, {1})", extent.Width, extent.Height);
                return extent;
            }
        }

        public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, int mipLevels)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands(m_device, m_graphicsCommandPool);

            ImageMemoryBarrier barrier = new ImageMemoryBarrier();
            barrier.OldLayout = oldLayout;
            barrier.NewLayout = newLayout;
            barrier.SrcQueueFamilyIndex = Constant.QueueFamilyIgnored;
            barrier.DstQueueFamilyIndex = Constant.QueueFamilyIgnored;
            barrier.Image = image;
            barrier.SubresourceRange = new ImageSubresourceRange();
            barrier.SubresourceRange.BaseMipLevel = 0;
            barrier.SubresourceRange.LevelCount = mipLevels;
            barrier.SubresourceRange.BaseArrayLayer = 0;
            barrier.SubresourceRange.LayerCount = 1;

            // if we're transitioning a depth/stencil image, change some settings
            if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                barrier.SubresourceRange.AspectMask = ImageAspects.Depth;

                if (HasStencilComponent(format))
                {
                    barrier.SubresourceRange.AspectMask |= ImageAspects.Stencil;
                }
            }
            else
            {
                barrier.SubresourceRange.AspectMask = ImageAspects.Color;
            }

            PipelineStages sourceStage;
            PipelineStages destinationStage;

            switch (oldLayout)
            {
                case ImageLayout.Preinitialized:
                case ImageLayout.Undefined:
                    /* Because the layout is undefined, it is not being used for anything before the
					 * transition, therefore we don't need to wait on any stage. Tell the driver to
					 * start the transition as soon as possible, giving it the freedom to choose the
					 * best place to do the transition.
					 */
                    sourceStage = PipelineStages.TopOfPipe;
                    barrier.SrcAccessMask = 0;
                    break;
                case ImageLayout.TransferDstOptimal:
                    /* The image is being used as a transfer destination, so don't start the transition
					 * until after the transfer write stage
					 */
                    sourceStage = PipelineStages.Transfer;
                    barrier.SrcAccessMask = Accesses.TransferWrite;
                    break;
                case ImageLayout.PresentSrcKhr:
                    sourceStage = PipelineStages.ColorAttachmentOutput;
                    barrier.SrcAccessMask = Accesses.ColorAttachmentRead;
                    break;
                case ImageLayout.ColorAttachmentOptimal:
                    sourceStage = PipelineStages.ColorAttachmentOutput;
                    barrier.SrcAccessMask = Accesses.ColorAttachmentWrite;
                    break;
                default:
                    Console.WriteLine("Image transition from unsupported layout ({0}) is not optimal", oldLayout);
                    goto case ImageLayout.General; // C# doesn't allow fallthrough if case has any code
                case ImageLayout.General:
                    /* We don't know what the image was used for before, so to be safe don't start the
					 * transition until the end of the pipe
					 */
                    sourceStage = PipelineStages.BottomOfPipe;
                    barrier.SrcAccessMask = 0;
                    break;
            }

            switch (newLayout)
            {
                case ImageLayout.Preinitialized:
                case ImageLayout.Undefined:
                    // transitioning image to preinitalized or undefined is not allowed
                    throw new InvalidOperationException();
                case ImageLayout.TransferDstOptimal:
                    /* The image is being used as a transfer destination, so make sure the transition
					 * is done before that stage
					 */
                    destinationStage = PipelineStages.Transfer;
                    barrier.DstAccessMask = Accesses.TransferWrite;
                    break;
                case ImageLayout.ShaderReadOnlyOptimal:
                    /* Image will be read by a shader, so make sure transition is done before then
					 */
                    // technically, the image may not be consumed by the vertex shader, and we could do the
                    // transition later, but we have no way of telling what shader this image will be for
                    destinationStage = PipelineStages.VertexShader;
                    barrier.DstAccessMask = Accesses.ShaderRead;
                    break;
                case ImageLayout.DepthStencilAttachmentOptimal:
                    /* Image will be used for depth tests, so make sure it's transitioned before the
					 * fragment test stage
					 */
                    destinationStage = PipelineStages.EarlyFragmentTests;
                    barrier.DstAccessMask = Accesses.DepthStencilAttachmentRead | Accesses.DepthStencilAttachmentWrite;
                    break;
                case ImageLayout.ColorAttachmentOptimal:
                    destinationStage = PipelineStages.ColorAttachmentOutput;
                    barrier.DstAccessMask = Accesses.ColorAttachmentWrite;
                    break;
                case ImageLayout.PresentSrcKhr:
                    destinationStage = PipelineStages.ColorAttachmentOutput;
                    barrier.DstAccessMask = Accesses.ColorAttachmentRead;
                    break;
                default:
                    Console.WriteLine("Image transition to unsupported layout ({0}) is not optimal", newLayout);
                    goto case ImageLayout.General;
                case ImageLayout.General:
                    /* We don't know what the image will be used for, so make sure the transition is done
					 * by the start of the pipe
					 */
                    destinationStage = PipelineStages.TopOfPipe;
                    barrier.DstAccessMask = 0;
                    break;
            }

            commandBuffer.CmdPipelineBarrier(sourceStage, destinationStage, 0,
                                             null, null, new[] { barrier });

            EndSingleTimeCommands(commandBuffer, m_graphicsCommandPool, m_graphicsQueue);
            m_device.WaitIdle();
        }

        public void CopyBufferToBuffer(VulkanCore.Buffer srcBuffer, long srcOffset, VulkanCore.Buffer dstBuffer, long dstOffset, long size)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands(m_device, m_transferCommandPool);

            BufferCopy region = new BufferCopy();
            region.SrcOffset = srcOffset;
            region.DstOffset = dstOffset;
            region.Size = size;

            commandBuffer.CmdCopyBuffer(srcBuffer, dstBuffer, new[] { region });

            EndSingleTimeCommands(commandBuffer, m_transferCommandPool, m_transferQueue);
        }

        public void CopyBufferToImage(VulkanCore.Buffer buffer, Image image, int width, int height)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands(m_device, m_transferCommandPool);

            BufferImageCopy region = new BufferImageCopy();
            region.BufferOffset = 0;
            region.BufferRowLength = 0;
            region.BufferImageHeight = 0;

            region.ImageSubresource.AspectMask = ImageAspects.Color;
            region.ImageSubresource.MipLevel = 0;
            region.ImageSubresource.BaseArrayLayer = 0;
            region.ImageSubresource.LayerCount = 1;

            region.ImageOffset.X = 0;
            region.ImageOffset.Y = 0;
            region.ImageOffset.Z = 0;
            region.ImageExtent.Width = width;
            region.ImageExtent.Height = height;
            region.ImageExtent.Depth = 1;

            commandBuffer.CmdCopyBufferToImage(buffer, image, ImageLayout.TransferDstOptimal, new[] { region });

            EndSingleTimeCommands(commandBuffer, m_transferCommandPool, m_transferQueue);
        }

        private CommandBuffer BeginSingleTimeCommands(Device device, CommandPool pool)
        {
            CommandBufferAllocateInfo allocInfo = new CommandBufferAllocateInfo();
            allocInfo.Level = CommandBufferLevel.Primary;
            allocInfo.CommandBufferCount = 1;

            m_poolLocks[pool.Handle].EnterLock();
            CommandBuffer commandBuffer = pool.AllocateBuffers(allocInfo)[0];

            CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
            beginInfo.Flags = CommandBufferUsages.OneTimeSubmit;

            commandBuffer.Begin(beginInfo);

            return commandBuffer;
        }

        private void EndSingleTimeCommands(CommandBuffer commandBuffer, CommandPool pool, Queue submitQueue)
        {
            /* Submit command buffer */
            commandBuffer.End();

            // create fence
            FenceCreateInfo fenceInfo = new FenceCreateInfo();
            Fence transferDone = m_device.CreateFence(fenceInfo);

            // submit
            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.CommandBuffers = new[] { commandBuffer.Handle };
            m_queueLocks[submitQueue.Handle].EnterLock();
            submitQueue.Submit(submitInfo, transferDone);
            m_queueLocks[submitQueue.Handle].ExitLock();
            m_poolLocks[pool.Handle].ExitLock();

            /* Wait for command buffer to finish */
            m_device.WaitFences(new[] { transferDone }, true);
        }

        private bool HasStencilComponent(Format format)
        {
            return format == Format.D32SFloatS8UInt || format == Format.D24UNormS8UInt;
        }

        private Format FindSupportedFormat(Format[] candidates, ImageTiling tiling, FormatFeatures features)
        {
            foreach (Format format in candidates)
            {
                FormatProperties properties = m_physicalDevice.GetFormatProperties(format);

                if (tiling == ImageTiling.Linear && (properties.LinearTilingFeatures & features) == features)
                {
                    return format;
                }
                else if (tiling == ImageTiling.Optimal && (properties.OptimalTilingFeatures & features) == features)
                {
                    return format;
                }
            }

            throw new Exception();
        }

        public void CreateBuffer(long size, BufferUsages usage, MemoryProperties requiredFlags, MemoryProperties preferredFlags, out VulkanCore.Buffer buffer, out VmaAllocation allocation)
        {
            BufferCreateInfo bufferInfo = new BufferCreateInfo();
            bufferInfo.Size = size;
            bufferInfo.Usage = usage;

            HashSet<int> queueIndicesSet = new HashSet<int>();
            queueIndicesSet.Add((int)m_deviceQueueFamilies.graphicsFamily);
            queueIndicesSet.Add((int)m_deviceQueueFamilies.transferFamily);
            int[] uniqueQueueIndices = new int[queueIndicesSet.Count];
            queueIndicesSet.CopyTo(uniqueQueueIndices);

            if (uniqueQueueIndices.Length > 1)
            {
                bufferInfo.SharingMode = SharingMode.Concurrent;
                bufferInfo.QueueFamilyIndices = uniqueQueueIndices;
            }
            else
            {
                bufferInfo.SharingMode = SharingMode.Exclusive;
            }

            VmaAllocationCreateInfo allocInfo = new VmaAllocationCreateInfo();
            allocInfo.usage = VmaMemoryUsage.VMA_MEMORY_USAGE_UNKNOWN;
            allocInfo.requiredFlags = requiredFlags;
            allocInfo.preferredFlags = preferredFlags;

            Result result = m_memoryAllocator.CreateBuffer(bufferInfo, allocInfo, out buffer, out allocation);
            if (result != Result.Success)
            {
                throw new VulkanException(result);
            }
        }

        /* Free a buffer
		 */
        public void FreeAllocation(VmaAllocation allocation)
        {
            m_memoryAllocator.FreeAllocation(allocation);
        }

        public void CreateImage2D(
            int width,
            int height,
            int mipLevels,
            Format format,
            ImageTiling tiling,
            ImageUsages usage,
            MemoryProperties requiredFlags,
            MemoryProperties preferredFlags,
            out Image image,
            out VmaAllocation allocation)
        {
            HashSet<int> queueIndicesSet = new HashSet<int>();
            queueIndicesSet.Add((int)m_deviceQueueFamilies.graphicsFamily);
            queueIndicesSet.Add((int)m_deviceQueueFamilies.transferFamily);
            int[] uniqueQueueIndices = new int[queueIndicesSet.Count];
            queueIndicesSet.CopyTo(uniqueQueueIndices);

            ImageCreateInfo imageInfo = new ImageCreateInfo();
            imageInfo.Flags = 0;
            imageInfo.ImageType = ImageType.Image2D;
            imageInfo.Format = format;
            imageInfo.Extent = new Extent3D();
            imageInfo.Extent.Width = width;
            imageInfo.Extent.Height = height;
            imageInfo.Extent.Depth = 1;
            imageInfo.MipLevels = mipLevels;
            imageInfo.ArrayLayers = 1;
            imageInfo.Samples = SampleCounts.Count1;
            imageInfo.Tiling = tiling;
            imageInfo.Usage = usage;
            imageInfo.InitialLayout = ImageLayout.Undefined;

            if (uniqueQueueIndices.Length > 1)
            {
                imageInfo.SharingMode = SharingMode.Concurrent;
                imageInfo.QueueFamilyIndices = uniqueQueueIndices;
            }
            else
            {
                imageInfo.SharingMode = SharingMode.Exclusive;
            }

            VmaAllocationCreateInfo allocInfo = new VmaAllocationCreateInfo();
            allocInfo.usage = VmaMemoryUsage.VMA_MEMORY_USAGE_UNKNOWN;
            allocInfo.requiredFlags = requiredFlags;
            allocInfo.preferredFlags = preferredFlags;

            Result result = m_memoryAllocator.CreateImage(imageInfo, allocInfo, out image, out allocation);
            if (result != Result.Success)
            {
                throw new VulkanException(result);
            }
        }

        /* Block until the command buffer and swapchain image at index are idle,
		 * and get the lock to avoid submitting the buffer for now
		 */
        public void WaitForSwapchainBufferIdleAndLock(int index)
        {
            m_inFlightFenceLocks[index].EnterLock();
            if (m_frameInFlight[index])
            {
                m_inFlightFences[index].Wait();
            }
            m_inFlightFenceLocks[index].ExitLock();
            m_swapchainBufferLocks[index].EnterLock();
        }

        public void ReleaseSwapchainBufferLock(int index)
        {
            m_swapchainBufferLocks[index].ExitLock();
        }

        public bool DoesThreadOwnSwapchainLock(int index){
            return m_swapchainBufferLocks[index].IsLockedByThread();
        }

        /* Waits for the whole graphics pipeline to be idle, and prevents
		 * further commands from being submitted to pipeline (same as calling
		 * WaitForSwapchainBufferIdleAndLock() on every swapchain index)
		 */
        public void WaitForDeviceIdleAndLock()
        {
            ThreadLock.EnterMultiple(m_swapchainBufferLocks);
        }

        public void ReleaseDeviceIdleLock()
        {
            m_device.WaitIdle();
            ThreadLock.ExitMultiple(m_swapchainBufferLocks);
        }

        /* Submit the command buffer to the graphics queue
		 */
        public void Draw()
        {
            SubmitInfo submitInfo = new SubmitInfo();
            submitInfo.WaitSemaphores = new[] { m_imageAvailableSemaphores[m_currentFrame].Handle };
            submitInfo.WaitDstStageMask = new[] { PipelineStages.ColorAttachmentOutput };
            submitInfo.CommandBuffers = new[] { m_swapchainCommandBuffers[m_currentFrame].Handle };
            submitInfo.SignalSemaphores = new[] { m_renderFinishedSemaphores[m_currentFrame].Handle };

            // get lock for queue
            ThreadLock.EnterMultiple(m_queueLocks[m_graphicsQueue.Handle], m_inFlightFenceLocks[m_currentFrame]);
			m_frameInFlight[m_currentFrame] = true;
            m_graphicsQueue.Submit(submitInfo, m_inFlightFences[m_currentFrame]);
            ThreadLock.ExitMultiple(m_queueLocks[m_graphicsQueue.Handle], m_inFlightFenceLocks[m_currentFrame]);
            m_poolLocks[m_swapchainCommandBuffers[m_currentFrame].Parent.Handle].ExitLock();
        }

        public void Present()
        {
            PresentInfoKhr presentInfo = new PresentInfoKhr();
            presentInfo.WaitSemaphores = new[] { m_renderFinishedSemaphores[m_currentFrame].Handle };
            presentInfo.Swapchains = new[] { m_swapchain.Handle };
            presentInfo.ImageIndices = new[] { m_frameIndex };
            presentInfo.Results = null;

            m_presentQueue.PresentKhr(presentInfo);

            // wait for frame to finish rendering
            m_inFlightFenceLocks[m_currentFrame].EnterLock();
            m_inFlightFences[m_currentFrame].Wait();
			m_frameInFlight[m_currentFrame] = false;
            m_inFlightFenceLocks[m_currentFrame].ExitLock();

            // unlock mutex
            m_swapchainBufferLocks[m_currentFrame].ExitLock();

            m_currentFrame = (m_currentFrame + 1) % m_swapchainImages.Length;
            m_frameIndex = m_swapchain.AcquireNextImage(-1, m_imageAvailableSemaphores[m_currentFrame]);
        }


        public VulkanPipeline CreatePipeline(VulkanPipeline.VulkanPipelineCreateInfo createInfo)
        {
            return new VulkanPipeline(this, createInfo);
        }

        public VulkanShader CreateShader(VulkanShader.ShaderCreateInfo createInfo)
        {
            return new VulkanShader(this, createInfo);
        }

        public CommandBuffer StartRecordingSwapchainCommandBuffer(out int currentFrame)
        {
            // get lock
            m_swapchainBufferLocks[m_currentFrame].EnterLock();

            m_inFlightFenceLocks[m_currentFrame].EnterLock();
            m_inFlightFences[m_currentFrame].Wait();
            m_inFlightFences[m_currentFrame].Reset();
            m_inFlightFenceLocks[m_currentFrame].ExitLock();
            CommandBuffer currentBuffer = m_swapchainCommandBuffers[m_currentFrame];
            CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo();
            beginInfo.Flags = CommandBufferUsages.SimultaneousUse;
            m_poolLocks[currentBuffer.Parent.Handle].EnterLock();
            currentBuffer.Begin(beginInfo);
            currentFrame = m_currentFrame;
            return currentBuffer;
        }

        public void FinalizeSwapchainCommandBuffer(CommandBuffer swapchainBuffer)
        {
            swapchainBuffer.End();

            // release lock
            //m_swapchainBufferLock[m_currentFrame].ReleaseMutex();
        }

        public Extent2D GetSwapchainImageExtent()
        {
            return m_swapchainImageExtent;
        }

        public Format GetSwapchainImageFormat()
        {
            return m_swapchainImageFormat;
        }

        public Format GetDepthImageFormat()
        {
            return m_depthImageFormat;
        }

        public int GetSwapchainImageCount()
        {
            return m_swapchainImages.Length;
        }

        public ImageView GetSwapchainImageView(int index)
        {
            return m_swapchainImageViews[index];
        }

        public ImageView GetDepthImageView()
        {
            return m_depthImageView;
        }

        public Device GetDevice()
        {
            return m_device;
        }

        public PhysicalDevice GetPhysicalDevice()
        {
            return m_physicalDevice;
        }

        public IWindowManager GetWindowManager()
        {
            return m_window;
        }

        private class DeviceQueueFamilies
        {
            public uint graphicsFamily = uint.MaxValue;
            public uint transferFamily = uint.MaxValue;
            public uint presentFamily = uint.MaxValue;

            public bool IsComplete()
            {
                return ((graphicsFamily < uint.MaxValue) &&
                        (transferFamily < uint.MaxValue) &&
                        (presentFamily < uint.MaxValue));
            }
        }

        private struct SwapChainSupportDetails
        {
            public SurfaceCapabilitiesKhr capabilities;
            public SurfaceFormatKhr[] formats;
            public PresentModeKhr[] presentModes;
        }
    }
}
