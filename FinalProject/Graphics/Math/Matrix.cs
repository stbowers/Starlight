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

        public BasicMatrix(int n, int m, T[] data): this(n, m)
        {
            if (data.Length != m_data.Length)
            {
                throw new ApplicationException();
            }

            data.CopyTo(m_data, 0);
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

        public long PrimativeSizeOf
        {
            get
            {
                return Marshal.SizeOf(typeof(T)) * m_data.Length;
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
    public class Mat4 : BasicMatrix<float>
    {
        // create a 4x4 matrix
        public Mat4() : base(4, 4)
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

        // Math functions
        public static Mat4 operator*(Mat4 left, Mat4 right)
        {
            Mat4 result = new Mat4();

            result[0, 0] = (left[0, 0] * right[0, 0]) + (left[1, 0] * right[0, 1]) + (left[2, 0] * right[0, 2]) + (left[3, 0] * right[0, 3]);
            result[1, 0] = (left[0, 1] * right[1, 0]) + (left[1, 1] * right[1, 1]) + (left[2, 1] * right[1, 2]) + (left[3, 1] * right[1, 3]);
            result[2, 0] = (left[0, 2] * right[2, 0]) + (left[1, 2] * right[2, 1]) + (left[2, 2] * right[2, 2]) + (left[3, 2] * right[2, 3]);
            result[3, 0] = (left[0, 3] * right[3, 0]) + (left[1, 3] * right[3, 1]) + (left[2, 3] * right[3, 2]) + (left[3, 3] * right[3, 3]);
            result[0, 1] = (left[0, 0] * right[0, 0]) + (left[1, 0] * right[0, 1]) + (left[2, 0] * right[0, 2]) + (left[3, 0] * right[0, 3]);
            result[1, 1] = (left[0, 1] * right[1, 0]) + (left[1, 1] * right[1, 1]) + (left[2, 1] * right[1, 2]) + (left[3, 1] * right[1, 3]);
            result[2, 1] = (left[0, 2] * right[2, 0]) + (left[1, 2] * right[2, 1]) + (left[2, 2] * right[2, 2]) + (left[3, 2] * right[2, 3]);
            result[3, 1] = (left[0, 3] * right[3, 0]) + (left[1, 3] * right[3, 1]) + (left[2, 3] * right[3, 2]) + (left[3, 3] * right[3, 3]);
            result[0, 2] = (left[0, 0] * right[0, 0]) + (left[1, 0] * right[0, 1]) + (left[2, 0] * right[0, 2]) + (left[3, 0] * right[0, 3]);
            result[1, 2] = (left[0, 1] * right[1, 0]) + (left[1, 1] * right[1, 1]) + (left[2, 1] * right[1, 2]) + (left[3, 1] * right[1, 3]);
            result[2, 2] = (left[0, 2] * right[2, 0]) + (left[1, 2] * right[2, 1]) + (left[2, 2] * right[2, 2]) + (left[3, 2] * right[2, 3]);
            result[3, 2] = (left[0, 3] * right[3, 0]) + (left[1, 3] * right[3, 1]) + (left[2, 3] * right[3, 2]) + (left[3, 3] * right[3, 3]);
            result[0, 3] = (left[0, 0] * right[0, 0]) + (left[1, 0] * right[0, 1]) + (left[2, 0] * right[0, 2]) + (left[3, 0] * right[0, 3]);
            result[1, 3] = (left[0, 1] * right[1, 0]) + (left[1, 1] * right[1, 1]) + (left[2, 1] * right[1, 2]) + (left[3, 1] * right[1, 3]);
            result[2, 3] = (left[0, 2] * right[2, 0]) + (left[1, 2] * right[2, 1]) + (left[2, 2] * right[2, 2]) + (left[3, 2] * right[2, 3]);
            result[3, 3] = (left[0, 3] * right[3, 0]) + (left[1, 3] * right[3, 1]) + (left[2, 3] * right[3, 2]) + (left[3, 3] * right[3, 3]);

            return result;
        }

        public static Mat4 operator *(float left, Mat4 right)
        {
            Mat4 newMatrix = new Mat4();
            for (int i = 0; i < right.m_n; i++)
            {
                for (int j = 0; j < right.m_m; j++)
                {
                    newMatrix[i, j] = left * right[i, j];
                }
            }

            return newMatrix;
        }

        // Static functions for model, view, and projection matricies
        public static Mat4 LookAt(Vec3 eye, Vec3 center, Vec3 up)
        {
            Mat4 newMatrix = new Mat4();

            Vec3 f = center - eye;
            f.Normalize();
            Vec3 nUp = new Vec3(up.X, up.Y, up.Z);
            nUp.Normalize();
            Vec3 s = f.Cross(nUp);
            Vec3 ns = new Vec3(s.X, s.Y, s.Z);
            ns.Normalize();
            Vec3 u = ns.Cross(f);

            newMatrix[0, 0] = s.X;
            newMatrix[1, 0] = s.Y;
            newMatrix[2, 0] = s.Z;
            newMatrix[3, 0] = 0;
            newMatrix[0, 1] = u.X;
            newMatrix[1, 1] = u.Y;
            newMatrix[2, 1] = u.Z;
            newMatrix[3, 1] = 0;
            newMatrix[0, 2] = -f.X;
            newMatrix[1, 2] = -f.Y;
            newMatrix[2, 2] = -f.Z;
            newMatrix[3, 2] = 0;
            newMatrix[0, 3] = 0;
            newMatrix[1, 3] = 0;
            newMatrix[2, 3] = 0;
            newMatrix[3, 3] = 1.0f;

            return newMatrix;
        }

        public static Mat4 Perspective(float angle, float ratio, float near, float far)
        {
            Mat4 perspectiveMatrix = new Mat4();
            float tanHalfAngle = (float)System.Math.Tan(angle / 2);

            perspectiveMatrix[0, 0] = 1 / (ratio * tanHalfAngle);
            perspectiveMatrix[1, 1] = 1 / (tanHalfAngle);
            perspectiveMatrix[2, 2] = -(far + near) / (far - near);
            perspectiveMatrix[3, 2] = -1;
            perspectiveMatrix[3, 3] = -(2 * far * near) / (far - near);

            return perspectiveMatrix;
        }
	}
}
