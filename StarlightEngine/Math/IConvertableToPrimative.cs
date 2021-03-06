﻿using System;

namespace StarlightEngine.Math
{
	/* Interface defined on most math objects to get the underlying bytes for the data
	 */
	public interface IConvertableToPrimative
	{
		// Gets the bytes underlying the object's data (should be binary compatable with glm objects of the same type)
		byte[] Bytes { get; }

        // Get the size of the underlying data in bytes
        long PrimativeSizeOf { get;  }
	}
}
