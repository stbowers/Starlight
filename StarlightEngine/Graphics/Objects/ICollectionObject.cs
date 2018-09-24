namespace StarlightEngine.Graphics.Objects
{
	public interface ICollectionObject: IGraphicsObject
	{
		IGraphicsObject[] Objects { get; }
	}
}
