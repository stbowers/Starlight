using System;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Scenes;

namespace StarlightEngine.Graphics
{
    [Flags]
    public enum IRendererSpecialObjectFlags
    {
        RenderDebugOverlay
    }

    /* Struct to hold special references to objects the renderer may want to
	 * draw regardless of the scene (FPS counter, debug info, etc)
	 */
    public struct IRendererSpecialObjectRefs
    {
        public IGameObject DebugOverlay;
    }

    /* Classes implementing this interface can be used to render IGraphicsObjects
	 */
    public interface IRenderer
    {
        // display a scene
        void DisplayScene(Scene scene);

        // sets the special object references
        void SetSpecialObjectReferences(IRendererSpecialObjectRefs refs);

        // Set which special objects to render
        void SetSpecialObjectsFlags(IRendererSpecialObjectFlags flags);

        // Update objects and/or caches to speed up rendering
        void Update();

        // Render all objects
        void Render();

        // Present the rendered image
        void Present();
    }
}
