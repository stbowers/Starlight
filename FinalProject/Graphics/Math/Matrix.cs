using System;
using System.Runtime.InteropServices;

namespace FinalProject.Graphics.Math
{
	public class BasicMatrix<T> : IConvertableToPrimative
	{
		// data is stored in row-major order, ie data(x, y) = data[ (y * width) + x ]
		protected T[] m_data;
		protected int m_n, m_m;

		public BasicMatrix(int n, int m)
		{
			m_n = n;
			m_m = m;

			m_data = new T[n * m];
		}

		public byte[] Bytes
		{
			get
			{
				byte[] bytes = new byte[m_data.Length * Marshal.SizeOf(typeof(T))];
				Buffer.BlockCopy(m_data, 0, bytes, 0, bytes.Length);
				return bytes;
			}
		}

		public T this[int x, int y]
		{
			get
			{
				return m_data[(y * m_n) + x];
			}

			set
			{
				m_data[(y * m_n) + x] = value;
			}
		}
	}

	// a 4x4 square matrix
	public class Mat4: BasicMatrix<float>
	{
		// create a 4x4 matrix
		public Mat4(): base(4, 4)
		{
		}

		// create a 4x4 matrix and fill diagonal with k
		public Mat4(float k) : this()
		{
			for (int x = 0; x < 4; x++)
			{
				for (int y = 0; y < 4; y++)
				{
					if (x == y)
					{
						this[x, y] = k;
					}
				}
			}
		}
	}
}
