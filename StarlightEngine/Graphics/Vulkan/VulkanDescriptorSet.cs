using System;
using VulkanCore;
using System.Threading;

namespace StarlightEngine.Graphics.Vulkan
{
	public class VulkanDescriptorSet
	{
		VulkanAPIManager m_apiManager;
		DescriptorSet[] m_sets;
		int m_setIndex;

		public VulkanDescriptorSet(VulkanAPIManager apiManager, DescriptorSet[] sets, int setIndex)
		{
			m_apiManager = apiManager;
			m_sets = sets;
			m_setIndex = setIndex;
		}

		public DescriptorSet GetSet(int swapchainIndex)
		{
			return m_sets[swapchainIndex];
		}

		public int GetSetIndex()
		{
			return m_setIndex;
		}

		/* Updates all sets to use the new buffer info, if block is true blocks until all descriptors are updated
		 */
		public void UpdateBuffer(int binding, DescriptorBufferInfo bufferInfo, DescriptorType type, bool block)
		{
			// spawn new threads to update the descriptors
			Thread[] updateThreads = new Thread[m_sets.Length];
			for (int i = 0; i < m_sets.Length; i++)
			{
				updateThreads[i] = new Thread(UpdateBuffer);
				updateThreads[i].Start(new ValueTuple<int, DescriptorBufferInfo, DescriptorType, int>(binding, bufferInfo, type, i));
			}

			if (block)
			{
				foreach (Thread thread in updateThreads)
				{
					thread.Join();
				}
			}
		}

		/* Updates all sets to use the new image info, if block is true blocks until all descriptors are updated
		 */
		public void UpdateImage(int binding, DescriptorImageInfo imageInfo, DescriptorType type, bool block)
		{
			// spawn new threads to update the descriptors
			Thread[] updateThreads = new Thread[m_sets.Length];
			for (int i = 0; i < m_sets.Length; i++)
			{
				updateThreads[i] = new Thread(UpdateImage);
				updateThreads[i].Start(new ValueTuple<int, DescriptorImageInfo, DescriptorType, int>(binding, imageInfo, type, i));
			}

			if (block)
			{
				foreach (Thread thread in updateThreads)
				{
					thread.Join();
				}
			}
		}

		/* Updates buffer info for a set at a given index (blocks until set is not being used before updating)
		 */
		private void UpdateBuffer(Object obj)
		{
			ValueTuple<int, DescriptorBufferInfo, DescriptorType, int> tuple = (ValueTuple<int, DescriptorBufferInfo, DescriptorType, int>)obj;
			int binding = tuple.Item1;
			DescriptorBufferInfo bufferInfo = tuple.Item2;
			DescriptorType type = tuple.Item3;
			int index = tuple.Item4;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = m_sets[index];
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = type;
			descriptorWrite.BufferInfo = new[] { bufferInfo };

			m_apiManager.WaitForSwapchainBufferIdle(index);
			m_sets[index].Parent.UpdateSets(new[] { descriptorWrite });
		}

		/* Updates image info for a set at a given index (blocks until set is not being used before updating)
		 */
		private void UpdateImage(Object obj)
		{
			ValueTuple<int, DescriptorImageInfo, DescriptorType, int> tuple = (ValueTuple<int, DescriptorImageInfo, DescriptorType, int>)obj;
			int binding = tuple.Item1;
			DescriptorImageInfo imageInfo = tuple.Item2;
			DescriptorType type = tuple.Item3;
			int index = tuple.Item4;

			WriteDescriptorSet descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.DstSet = m_sets[index];
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorCount = 1;
			descriptorWrite.DescriptorType = type;
			descriptorWrite.ImageInfo = new[] { imageInfo };

			m_apiManager.WaitForSwapchainBufferIdle(index);
			m_sets[index].Parent.UpdateSets(new[] { descriptorWrite });
		}
	}
}
