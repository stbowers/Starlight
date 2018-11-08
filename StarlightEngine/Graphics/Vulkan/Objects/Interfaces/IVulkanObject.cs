using System;
using StarlightEngine.Math;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Vulkan.Objects.Interfaces
{
    /* Component of IGraphicsObjects which can be rendered by a Vulkan renderer. Gives access to any objects that might
	 * be required to render the object
	 */
    public interface IVulkanObject : IGraphicsObject
    {
        /// <summary>
        /// Update the mvp data used to draw this object. The final transformation
        /// will look something like:
        /// finalPt = projection * view * modelTransform * objectModelMatrix * inputPt
        /// </summary>
        /// <param name="projection">Projection matrix - transforms camera space into normalized screen space</param>
        /// <param name="view">View matrix - transforms world space into camera space</param>
        /// <param name="modelTransform">Model transform matrix - transforms this object's space into world space</param>
        void UpdateMVPData(FMat4 projection, FMat4 view, FMat4 modelTransform);
    }
}
