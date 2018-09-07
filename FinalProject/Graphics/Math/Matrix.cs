using System;
using System.Runtime.InteropServices;

namespace FinalProject.Graphics.Math
{
	public class BasicMatrix<T> : IConvertableToPrimative
	{
		// data is stored in column-major order, ie data(i, j) = data[ (j * height) + i ], since that's the format Vulkan uses
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

		public T this[int i, int j]
		{
			get
			{
				return m_data[(j * m_m) + i];
			}

			set
			{
				m_data[(j * m_m) + i] = value;
			}
		}

		public int Width
		{
			get
			{
				return m_n;
			}
		}

		public int Height
		{
			get
			{
				return m_m;
			}
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			foreach (T element in m_data)
			{
				hashCode = hashCode * 23 + element.GetHashCode();
			}

			return hashCode;
		}

		public override bool Equals(object obj)
		{
			bool equal = false;
			if (!(obj is BasicMatrix<T>) || ((BasicMatrix<T>)obj).m_data.Length != m_data.Length)
			{
				return false;
			}
			for (int i = 0; i < m_data.Length; i++)
			{
				equal &= m_data[i].Equals(((BasicMatrix<T>)obj).m_data[i]);
			}
			return equal;
		}

		// Delegates for basic operations on T
		public delegate T Zero();
		public delegate T One();
		public delegate T Add(T left, T right);
		public delegate T Multiply(T left, T right);

		// Math operations
		public static BasicMatrix<T> MultiplyMatricies(BasicMatrix<T> left, BasicMatrix<T> right, Add addFunction, Multiply multiplyFunction, Zero zeroFunction)
		{
			// Check if left and right are correct dimensions
			if (left.m_m != right.m_n)
			{
				throw new InvalidOperationException();
			}

			// create a new matrix for the product
			BasicMatrix<T> product = new BasicMatrix<T>(left.m_n, right.m_m);

			// product(i, j) = left(i, 0)*right(0, j) + ... + left(i, m-1)*right(m-1, j)
			for (int i = 0; i < product.m_m; i++)
			{
				for (int j = 0; j < product.m_n; j++)
				{
					T result = zeroFunction();
					for (int m = 0; m < left.m_m; m++)
					{
						result = addFunction(result, multiplyFunction(left[i, m], right[m, j]));
					}
					product[i, j] = result;
				}
			}

			return product;
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

            result[0, 0] = (left[0, 0] * right[0, 0]) + (left[0, 1] * right[1, 0]) + (left[0, 2] * right[2, 0]) + (left[0, 3] * right[3, 0]);
            result[0, 1] = (left[0, 0] * right[0, 1]) + (left[0, 1] * right[1, 1]) + (left[0, 2] * right[2, 1]) + (left[0, 3] * right[3, 1]);
            result[0, 2] = (left[0, 0] * right[0, 2]) + (left[0, 1] * right[1, 2]) + (left[0, 2] * right[2, 2]) + (left[0, 3] * right[3, 2]);
            result[0, 3] = (left[0, 0] * right[0, 3]) + (left[0, 1] * right[1, 3]) + (left[0, 2] * right[2, 3]) + (left[0, 3] * right[3, 3]);
            result[1, 0] = (left[1, 0] * right[0, 0]) + (left[1, 1] * right[1, 0]) + (left[1, 2] * right[2, 0]) + (left[1, 3] * right[3, 0]);
            result[1, 1] = (left[1, 0] * right[0, 1]) + (left[1, 1] * right[1, 1]) + (left[1, 2] * right[2, 1]) + (left[1, 3] * right[3, 1]);
            result[1, 2] = (left[1, 0] * right[0, 2]) + (left[1, 1] * right[1, 2]) + (left[1, 2] * right[2, 2]) + (left[1, 3] * right[3, 2]);
            result[1, 3] = (left[1, 0] * right[0, 3]) + (left[1, 1] * right[1, 3]) + (left[1, 2] * right[2, 3]) + (left[1, 3] * right[3, 3]);
            result[2, 0] = (left[2, 0] * right[0, 0]) + (left[2, 1] * right[1, 0]) + (left[2, 2] * right[2, 0]) + (left[2, 3] * right[3, 0]);
            result[2, 1] = (left[2, 0] * right[0, 1]) + (left[2, 1] * right[1, 1]) + (left[2, 2] * right[2, 1]) + (left[2, 3] * right[3, 1]);
            result[2, 2] = (left[2, 0] * right[0, 2]) + (left[2, 1] * right[1, 2]) + (left[2, 2] * right[2, 2]) + (left[2, 3] * right[3, 2]);
            result[2, 3] = (left[2, 0] * right[0, 3]) + (left[2, 1] * right[1, 3]) + (left[2, 2] * right[2, 3]) + (left[2, 3] * right[3, 3]);
            result[3, 0] = (left[3, 0] * right[0, 0]) + (left[3, 1] * right[1, 0]) + (left[3, 2] * right[2, 0]) + (left[3, 3] * right[3, 0]);
            result[3, 1] = (left[3, 0] * right[0, 1]) + (left[3, 1] * right[1, 1]) + (left[3, 2] * right[2, 1]) + (left[3, 3] * right[3, 1]);
            result[3, 2] = (left[3, 0] * right[0, 2]) + (left[3, 1] * right[1, 2]) + (left[3, 2] * right[2, 2]) + (left[3, 3] * right[3, 2]);
            result[3, 3] = (left[3, 0] * right[0, 3]) + (left[3, 1] * right[1, 3]) + (left[3, 2] * right[2, 3]) + (left[3, 3] * right[3, 3]);

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
            newMatrix[0, 1] = s.Y;
            newMatrix[0, 2] = s.Z;
            newMatrix[0, 3] = 0;
            newMatrix[1, 0] = u.X;
            newMatrix[1, 1] = u.Y;
            newMatrix[1, 2] = u.Z;
            newMatrix[1, 3] = 0;
            newMatrix[2, 0] = -f.X;
            newMatrix[2, 1] = -f.Y;
            newMatrix[2, 2] = -f.Z;
            newMatrix[2, 3] = 0;
            newMatrix[3, 0] = 0;
            newMatrix[3, 1] = 0;
            newMatrix[3, 2] = 0;
            newMatrix[3, 3] = 1.0f;

			Mat4 translationMatrix = new Mat4(1.0f);
			translationMatrix[0, 3] = -eye.X;
			translationMatrix[1, 3] = -eye.Y;
			translationMatrix[2, 3] = -eye.Z;
			newMatrix = newMatrix * translationMatrix;

            return newMatrix;
        }

        public static Mat4 Perspective(float angle, float ratio, float near, float far)
        {
            Mat4 perspectiveMatrix = new Mat4();
			float f = 1.0f / (float)System.Math.Tan(angle / 2);

			perspectiveMatrix[0, 0] = f / ratio;
			perspectiveMatrix[1, 1] = f;
            perspectiveMatrix[2, 2] = (far + near) / (near - far);
            perspectiveMatrix[3, 2] = -1;
            perspectiveMatrix[2, 3] = (2 * far * near) / (near - far);

            return perspectiveMatrix;
        }

		public static Mat4 Rotate(float angle, Vec3 axis)
		{
			Mat4 result = new Mat4();

			float s = (float)System.Math.Sin(angle);
			float c = (float)System.Math.Cos(angle);
			Vec3 nAxis = new Vec3(axis.X, axis.Y, axis.Z);
			nAxis.Normalize();
			float x = nAxis.X;
			float y = nAxis.Y;
			float z = nAxis.Z;

			result[0, 0] = (x * x * (1 - c)) + (c);
			result[0, 1] = (x * y * (1 - c)) - (z * s);
			result[0, 2] = (x * z * (1 - c)) + (y * s);
			result[0, 3] = 0;
			result[1, 0] = (y * x * (1 - c)) + (z * s);
			result[1, 1] = (y * y * (1 - c)) + (c);
			result[1, 2] = (y * z * (1 - c)) - (x * s);
			result[1, 3] = 0;
			result[2, 0] = (x * z * (1 - c)) - (y * s);
			result[2, 1] = (y * z * (1 - c)) + (x * s);
			result[2, 2] = (z * z * (1 - c)) + (c);
			result[2, 3] = 0;
			result[3, 0] = 0;
			result[3, 1] = 0;
			result[3, 2] = 0;
			result[3, 3] = 1;

			return result;
		}
	}
}
