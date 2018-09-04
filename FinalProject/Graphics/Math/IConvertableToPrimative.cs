using System;
namespace FinalProject.Graphics.Math
{
	/* Interface defined on most math objects to get the underlying bytes for the data
	 */
	public interface IConvertableToPrimative
	{
		// Gets the bytes underlying the object's data (should be binary compatable with glm objects of the same type)
		byte[] Bytes { get; }
	}
}
