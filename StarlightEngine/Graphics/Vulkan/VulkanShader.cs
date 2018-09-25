using System;
using System.IO;
using System.Collections.Generic;
using VulkanCore;

namespace StarlightEngine.Graphics.Vulkan
{
	// list of data types that may be passed to shaders, and their size
	public enum ShaderTypes
	{
		vec2,
		vec3,
		vec4,
	}

	static class ShaderTypesExtensions
	{
		public static Format GetFormat(this ShaderTypes type)
		{
			switch (type)
			{
				case ShaderTypes.vec2:
					return Format.R32G32SFloat;
				case ShaderTypes.vec3:
					return Format.R32G32B32SFloat;
				case ShaderTypes.vec4:
					return Format.R32G32B32A32SFloat;
			}
			return Format.Undefined;
		}

		public static int GetSize(this ShaderTypes type)
		{
			switch (type)
			{
				case ShaderTypes.vec2:
					return 2 * 4;
				case ShaderTypes.vec3:
					return 3 * 4;
				case ShaderTypes.vec4:
					return 4 * 4;
			}
			return 0;
		}
	}

	public struct ShaderUniformInputInfo
	{
		public int @set;
		public int binding;
		public DescriptorType type;
        public ShaderStages stage;
	}

	public class VulkanShader
	{
		public struct ShaderCreateInfo
		{
			public string vertexShaderFile;
            public string fragmentShaderFile;
			public string vertexEntryPoint;
            public string fragmentEntryPoint;

			/* Jagged array specifing all inputs to the vertex shader, where
			 * inputs[binding][index] is the type of data expected at any given index for any given binding
			 */
			public ShaderTypes[][] inputs;

			/* Array of info for uniform inputs to the shader
			 */
			public ShaderUniformInputInfo[] uniformInputInfos;
		}

		// How many descriptor sets should a pool be able to allocate by default?
		private const int DESCRIPTOR_SET_POOL_PREFERRED_SIZE = 32;

		private VulkanAPIManager m_apiManager;
		private byte[] m_vertexCode;
        private byte[] m_fragmentCode;
		private ShaderModule m_vertexModule;
        private ShaderModule m_fragmentModule;
		private string m_vertexEntryPoint;
		private string m_fragmentEntryPoint;
		private VertexInputBindingDescription[] m_inputBindingDescriptions;
		private List<VertexInputAttributeDescription> m_inputAttributeDescriptions = new List<VertexInputAttributeDescription>();
		private List<DescriptorSetLayout> m_uniformInputLayouts = new List<DescriptorSetLayout>();
		private Dictionary<int, DescriptorSetBlock> m_descriptorSetPools;

		public VulkanShader(VulkanAPIManager apiManager, ShaderCreateInfo createInfo)
		{
			m_apiManager = apiManager;
			Device device = m_apiManager.GetDevice();
			m_vertexCode = File.ReadAllBytes(createInfo.vertexShaderFile);
			m_fragmentCode = File.ReadAllBytes(createInfo.fragmentShaderFile);

			ShaderModuleCreateInfo vertexModuleCreateInfo = new ShaderModuleCreateInfo();
			vertexModuleCreateInfo.Code = m_vertexCode;
			ShaderModuleCreateInfo fragmentModuleCreateInfo = new ShaderModuleCreateInfo();
			fragmentModuleCreateInfo.Code = m_fragmentCode;

			m_vertexModule = device.CreateShaderModule(vertexModuleCreateInfo);
			m_fragmentModule = device.CreateShaderModule(fragmentModuleCreateInfo);

            m_vertexEntryPoint = createInfo.vertexEntryPoint;
            m_fragmentEntryPoint = createInfo.fragmentEntryPoint;

			if (createInfo.inputs != null)
			{
				m_inputBindingDescriptions = new VertexInputBindingDescription[createInfo.inputs.Length];
				for (int binding = 0; binding < createInfo.inputs.Length; binding++)
				{
					m_inputBindingDescriptions[binding] = new VertexInputBindingDescription();
					m_inputBindingDescriptions[binding].Binding = binding;
					m_inputBindingDescriptions[binding].InputRate = VertexInputRate.Vertex;
					m_inputBindingDescriptions[binding].Stride = 0;

					for (int attribute = 0; attribute < createInfo.inputs[binding].Length; attribute++)
					{
						VertexInputAttributeDescription attributeDescription = new VertexInputAttributeDescription();
						attributeDescription.Binding = binding;
						attributeDescription.Location = attribute;
						attributeDescription.Format = createInfo.inputs[binding][attribute].GetFormat();
						attributeDescription.Offset = m_inputBindingDescriptions[binding].Stride;
						m_inputBindingDescriptions[binding].Stride += createInfo.inputs[binding][attribute].GetSize();
						m_inputAttributeDescriptions.Add(attributeDescription);
					}
				}
			}

			// Create descriptor layouts from uniform inputs
			if (createInfo.uniformInputInfos != null)
			{
				Dictionary<int, List<DescriptorSetLayoutBinding>> descriptorSetLayoutBindings = new Dictionary<int, List<DescriptorSetLayoutBinding>>();
				foreach (ShaderUniformInputInfo info in createInfo.uniformInputInfos)
				{
					DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding();
					binding.Binding = info.binding;
					binding.DescriptorType = info.type;
					binding.DescriptorCount = 1;
					binding.StageFlags = info.stage;
					binding.ImmutableSamplers = null;

					List<DescriptorSetLayoutBinding> setBindings;
					if (descriptorSetLayoutBindings.ContainsKey(info.set))
					{
						setBindings = descriptorSetLayoutBindings[info.set];
					}
					else
					{
						setBindings = new List<DescriptorSetLayoutBinding>();
						descriptorSetLayoutBindings.Add(info.set, setBindings);
					}
					setBindings.Add(binding);
				}

				m_descriptorSetPools = new Dictionary<int, DescriptorSetBlock>();

				foreach (var setBinding in descriptorSetLayoutBindings)
				{
					List<DescriptorSetLayoutBinding> setBindings = setBinding.Value;
					DescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo();
					descriptorSetLayoutCreateInfo.Bindings = setBindings.ToArray();

					DescriptorSetLayout setLayout = device.CreateDescriptorSetLayout(descriptorSetLayoutCreateInfo);
					m_uniformInputLayouts.Add(setLayout);

					DescriptorType[] descriptorTypes = new DescriptorType[setBindings.Count];
					for (int j = 0; j < setBindings.Count; j++)
					{
						descriptorTypes[j] = setBindings[j].DescriptorType;
					}
					m_descriptorSetPools[setBinding.Key] = new DescriptorSetBlock(device, setLayout, descriptorTypes, DESCRIPTOR_SET_POOL_PREFERRED_SIZE);
				}
			}
		}

		public VertexInputBindingDescription[] GetInputBindingDescriptions()
		{
			return m_inputBindingDescriptions;
		}

		public VertexInputAttributeDescription[] GetInputAttributeDescriptions()
		{
			return m_inputAttributeDescriptions.ToArray();
		}

		public DescriptorSet[] AllocateDescriptorSets(int setIndex, int numSets)
		{
			return m_descriptorSetPools[setIndex].AllocateDescriptorSets(numSets);
		}

		public long[] GetDescriptorSetLayouts()
		{
			long[] layoutHandles = new long[m_uniformInputLayouts.Count];
			for (int i = 0; i < layoutHandles.Length; i++)
			{
				layoutHandles[i] = m_uniformInputLayouts[i].Handle;
			}
			return layoutHandles;
		}

		public ShaderModule VertexModule
		{
			get
			{
				return m_vertexModule;
			}
		}

        public ShaderModule FragmentModule
        {
            get
            {
                return m_fragmentModule;
            }
        }

		public string VertexEntryPoint
		{
			get
			{
				return m_vertexEntryPoint;
			}
		}

		public string FragmentEntryPoint
		{
			get
			{
				return m_fragmentEntryPoint;
			}
		}

		/* Since descriptor set pools must be made with a limited number of allocations, and that limit can't be changed later,
		 * this class is used to make a linked list of pools, which can be expanded whenever the end is full
		 */
		private class DescriptorSetBlock
		{
			DescriptorPool m_pool;
			DescriptorSetLayout m_setLayout;
			DescriptorType[] m_types;
			int m_maxAllocations;
			int m_numAllocations;
			Device m_device;
			DescriptorSetBlock m_next;

			public DescriptorSetBlock(Device device, DescriptorSetLayout setLayout, DescriptorType[] types, int maxAllocations)
			{
				m_device = device;
				m_setLayout = setLayout;
				m_types = types;
				m_maxAllocations = maxAllocations;
				m_numAllocations = 0;
				m_next = null;

				DescriptorPoolSize[] poolSizes = new DescriptorPoolSize[types.Length];
				for (int i = 0; i < types.Length; i++)
				{
					poolSizes[i].Type = types[i];
					poolSizes[i].DescriptorCount = maxAllocations;
				}

				DescriptorPoolCreateInfo poolInfo = new DescriptorPoolCreateInfo();
				poolInfo.Flags = DescriptorPoolCreateFlags.FreeDescriptorSet;
				poolInfo.MaxSets = maxAllocations;
				poolInfo.PoolSizes = poolSizes;
				m_pool = device.CreateDescriptorPool(poolInfo);
			}

			public DescriptorSet[] AllocateDescriptorSets(int numDescriptorSets)
			{
				DescriptorSetBlock currentBlock = this;
				while (currentBlock.m_numAllocations + numDescriptorSets > currentBlock.m_maxAllocations)
				{
					currentBlock = currentBlock.m_next;
					if (currentBlock == null)
					{
						int newBlockNumAllocations = System.Math.Max(m_maxAllocations, numDescriptorSets);
						currentBlock = new DescriptorSetBlock(m_device, m_setLayout, m_types, newBlockNumAllocations);
					}
				}
				// the current block has enough space, get it's pool to allocate from
				DescriptorPool pool = currentBlock.m_pool;

				long[] layouts = new long[numDescriptorSets];
				for (int i = 0; i < numDescriptorSets; i++)
				{
					layouts[i] = m_setLayout;
				}

				DescriptorSetAllocateInfo allocInfo = new DescriptorSetAllocateInfo();
				allocInfo.DescriptorSetCount = numDescriptorSets;
				allocInfo.SetLayouts = layouts;

				return pool.AllocateSets(allocInfo);
			}
		}
	}
}
