using System;
using System.Collections.Generic;
using StarlightEngine.Graphics.Vulkan.Objects.Components;

namespace StarlightEngine.Graphics.Vulkan
{
    /// <summary>
    /// Helper class for loading textures
    /// </summary>
    public static class VulkanTextureCache
    {
        private static Dictionary<string, VulkanTexture> m_loadedTextures = new Dictionary<string, VulkanTexture>();

        /// <summary>
        /// Get the texture with the given name, and if it hasn't been loaded
        /// yet load it with the given settings
        /// </summary>
        public static VulkanTexture GetTexture(string texture, VulkanTextureCreateInfo createInfo)
        {
            if (m_loadedTextures.ContainsKey(texture))
            {
                return m_loadedTextures[texture];
            }

            // else, load texture and return it
            VulkanTexture newTexture = new VulkanTexture(createInfo);
            m_loadedTextures.Add(texture, newTexture);
            return newTexture;
        }
    }
}