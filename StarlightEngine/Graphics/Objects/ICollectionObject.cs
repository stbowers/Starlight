namespace StarlightEngine.Graphics.Objects
{
    public interface ICollectionObject : IGraphicsObject, IParent
    {
        IGraphicsObject[] Objects { get; }
    }
}
