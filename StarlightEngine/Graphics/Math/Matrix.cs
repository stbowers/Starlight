using System;
using System.Runtime.InteropServices;

namespace StarlightEngine.Graphics.Math
{
	// Defines simple math operations for a type T
	public struct SimpleOperations<T>
	{
		public delegate T Zero();
		public delegate T Identity();
		public delegate T Add(T left, T right);
		public delegate T Subtract(T left, T right);
		public delegate T Multiply(T left, T right);
		public delegate T Divide(T left, T right);
		public delegate T Pow(T @base, float exponent);

		public Zero zero;
		public Identity identity;
		public Add add;
		public Subtract subtract;
		public Multiply multiply;
		public Divide divide;
		public Pow pow;

		public SimpleOperations(Zero zero, Identity identity, Add add, Subtract subtract, Multiply multiply, Divide divide, Pow pow)
		{
			this.zero = zero;
			this.identity = identity;
			this.add = add;
			this.subtract = subtract;
			this.multiply = multiply;
			this.divide = divide;
			this.pow = pow;
		}
	}

	public struct FloatOperations
	{
		public static SimpleOperations<float> Operations = new SimpleOperations<float>(Zero, Identity, Add, Subtract, Multiply, Divide, Pow);

		public static float Zero()
		{
			return 0.0f;
		}

		public static float Identity()
		{
			return 1.0f;
		}

		public static float Add(float left, float right)
		{
			return left + right;
		}

		public static float Subtract(float left, float right)
		{
			return left - right;
		}

		public static float Multiply(float left, float right)
		{
			return left * right;
		}

		public static float Divide(float left, float right)
		{
			return left / right;
		}

		public static float Pow(float @base, float exponent)
		{
			return (float)System.Math.Pow(@base, exponent);
		}
	}

	public struct IntOperations
	{
		public static SimpleOperations<int> Operations = new SimpleOperations<int>(Zero, Identity, Add, Subtract, Multiply, Divide, Pow);

		public static int Zero()
		{
			return 0;
		}

		public static int Identity()
		{
			return 1;
		}

		public static int Add(int left, int right)
		{
			return left + right;
		}

		public static int Subtract(int left, int right)
		{
			return left - right;
		}

		public static int Multiply(int left, int right)
		{
			return left * right;
		}

		public static int Divide(int left, int right)
		{
			return left / right;
		}

		public static int Pow(int @base, float exponent)
		{
			return (int)System.Math.Round(System.Math.Pow(@base, exponent));
		}
	}

	public class BasicMatrix<T> : IConvertableToPrimative
	{
		// data is stored in column-major order, ie data(i, j) = data[ (j * height) + i ], since that's the format Vulkan uses
		protected T[] m_data;
		protected int m_n, m_m;
		protected SimpleOperations<T> m_operations;

		public BasicMatrix(int n, int m, SimpleOperations<T> operations)
		{
			m_n = n;
			m_m = m;

			m_data = new T[n * m];

			m_operations = operations;
		}

		public BasicMatrix(int n, int m, T[] data, SimpleOperations<T> operations): this(n, m, operations)
        {
            if (data.Length != m_data.Length)
            {
                throw new ApplicationException();
            }

            data.CopyTo(m_data, 0);
        }

		public T[] Data
		{
			get
			{
				return m_data;
			}
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

		public T this[int j, int i]
		{
			get
			{
				return m_data[(i * m_n) + j];
			}

			set
			{
				m_data[(i * m_n) + j] = value;
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

		// Math operations
		public static BasicMatrix<T> operator*(BasicMatrix<T> left, BasicMatrix<T> right)
		{
			// Check if left and right are correct dimensions
			if (left.m_m != right.m_n)
			{
				throw new InvalidOperationException();
			}

			// Get set of simple operations
			SimpleOperations<T> operations = left.m_operations;

			// create a new matrix for the product
			BasicMatrix<T> product = new BasicMatrix<T>(left.m_n, right.m_m, operations);

			// product(i, j) = left(i, 0)*right(0, j) + ... + left(i, m-1)*right(m-1, j)
			for (int i = 0; i < product.m_m; i++)
			{
				for (int j = 0; j < product.m_n; j++)
				{
					T result = operations.zero();
					for (int m = 0; m < left.m_m; m++)
					{
						result = operations.add(result, operations.multiply(left[i, m], right[m, j]));
					}
					product[i, j] = result;
				}
			}

			return product;
		}

		public static BasicMatrix<T> operator +(BasicMatrix<T> left, BasicMatrix<T> right)
		{
			// Check if left and right are correct dimensions
			if (left.m_m != right.m_m || left.m_n != right.m_n)
			{
				throw new InvalidOperationException();
			}

			// Get set of simple operations
			SimpleOperations<T> operations = left.m_operations;

			// create a new matrix for the sum
			BasicMatrix<T> sum = new BasicMatrix<T>(left.m_n, left.m_m, operations);

			// sum(i, j) = left(i, j)+right(i, j)
			for (int i = 0; i < sum.m_m; i++)
			{
				for (int j = 0; j < sum.m_n; j++)
				{
					sum[i, j] = operations.add(left[i, j], right[i, j]);
				}
			}

			return sum;
		}

		public static BasicMatrix<T> operator -(BasicMatrix<T> left, BasicMatrix<T> right)
		{
			// Check if left and right are correct dimensions
			if (left.m_m != right.m_m || left.m_n != right.m_n)
			{
				throw new InvalidOperationException();
			}

			// Get set of simple operations
			SimpleOperations<T> operations = left.m_operations;

			// create a new matrix for the difference
			BasicMatrix<T> difference = new BasicMatrix<T>(left.m_n, left.m_m, operations);

			// sum(i, j) = left(i, j)+right(i, j)
			for (int i = 0; i < difference.m_m; i++)
			{
				for (int j = 0; j < difference.m_n; j++)
				{
					difference[i, j] = operations.subtract(left[i, j], right[i, j]);
				}
			}

			return difference;
		}
	}

	// float matricies
	public class FMat : BasicMatrix<float>
	{
		public FMat(int n, int m) : base(n, m, FloatOperations.Operations)
		{
		}

		public FMat(int n, int m, float[] data) : base(n, m, data, FloatOperations.Operations)
		{
		}

		public FMat(int n, int m, BasicMatrix<float> copyFrom) : base(n, m, copyFrom.Data, FloatOperations.Operations)
		{
		}
	}

    // a 4x4 square matrix
    public class FMat4 : FMat
    {
        // create a 4x4 matrix
		public FMat4() : base(4, 4)
        {
        }

		public FMat4(float[] data) : base(4, 4, data)
		{
		}

		public FMat4(BasicMatrix<float> copyFrom) : base(4, 4, copyFrom)
		{
		}

		// create a 4x4 matrix and fill diagonal with k
		public FMat4(float k) : this()
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
        public static FMat4 operator*(FMat4 left, FMat4 right)
        {
            FMat4 result = new FMat4();

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

        public static FMat4 operator*(float left, FMat4 right)
        {
            FMat4 newMatrix = new FMat4();
            for (int i = 0; i < right.m_n; i++)
            {
                for (int j = 0; j < right.m_m; j++)
                {
                    newMatrix[i, j] = left * right[i, j];
                }
            }

            return newMatrix;
        }

		public static FVec4 operator *(FMat4 left, FVec4 right)
		{
			FVec4 result = new FVec4();

			result[0] = (left[0, 0] * right[0]) + (left[0, 1] * right[1]) + (left[0, 2] * right[2]) + (left[0, 3] * right[3]);
			result[1] = (left[1, 0] * right[0]) + (left[1, 1] * right[1]) + (left[1, 2] * right[2]) + (left[1, 3] * right[3]);
			result[2] = (left[2, 0] * right[0]) + (left[2, 1] * right[1]) + (left[2, 2] * right[2]) + (left[2, 3] * right[3]);
			result[3] = (left[3, 0] * right[0]) + (left[3, 1] * right[1]) + (left[3, 2] * right[2]) + (left[3, 3] * right[3]);

			return result;
		}

		public static FMat4 operator+(FMat4 left, FMat4 right)
		{
			BasicMatrix<float> l = left as BasicMatrix<float>;
			BasicMatrix<float> r = right as BasicMatrix<float>;
			BasicMatrix<float> sum = l + r;
			return new FMat4(sum);
		}

		public static FMat4 Invert(FMat4 matrix)
		{
			FMat4 newMatrix = new FMat4();

			return newMatrix;
		}

        // Static functions for model, view, and projection matricies
        public static FMat4 LookAt(FVec3 eye, FVec3 center, FVec3 up)
        {
            FMat4 newMatrix = new FMat4();

            FVec3 f = center - eye;
			f.Normalize();
            FVec3 nUp = up.XYZ();
            nUp.Normalize();
            FVec3 s = f.Cross(nUp);
            FVec3 ns = s.XYZ();
            ns.Normalize();
            FVec3 u = ns.Cross(f);

            newMatrix[0, 0] = s.X();
            newMatrix[0, 1] = s.Y();
            newMatrix[0, 2] = s.Z();
            newMatrix[0, 3] = 0;
            newMatrix[1, 0] = u.X();
            newMatrix[1, 1] = u.Y();
            newMatrix[1, 2] = u.Z();
            newMatrix[1, 3] = 0;
            newMatrix[2, 0] = -f.X();
            newMatrix[2, 1] = -f.Y();
            newMatrix[2, 2] = -f.Z();
            newMatrix[2, 3] = 0;
            newMatrix[3, 0] = 0;
            newMatrix[3, 1] = 0;
            newMatrix[3, 2] = 0;
            newMatrix[3, 3] = 1.0f;

			FMat4 translationMatrix = new FMat4(1.0f);
			translationMatrix[0, 3] = -eye.X();
			translationMatrix[1, 3] = -eye.Y();
			translationMatrix[2, 3] = -eye.Z();
			newMatrix = newMatrix * translationMatrix;

            return newMatrix;
        }

        public static FMat4 Perspective(float angle, float ratio, float near, float far)
        {
            FMat4 perspectiveMatrix = new FMat4();
			float f = 1.0f / (float)System.Math.Tan(angle / 2);

			perspectiveMatrix[0, 0] = f / ratio;
			perspectiveMatrix[1, 1] = f;
            perspectiveMatrix[2, 2] = (far + near) / (near - far);
            perspectiveMatrix[3, 2] = -1;
            perspectiveMatrix[2, 3] = (2 * far * near) / (near - far);

            return perspectiveMatrix;
        }

		public static FMat4 Rotate(float angle, FVec3 axis)
		{
			FMat4 result = new FMat4();

			float s = (float)System.Math.Sin(angle);
			float c = (float)System.Math.Cos(angle);
			FVec3 nAxis = axis.XYZ();
			nAxis.Normalize();
			float x = nAxis.X();
			float y = nAxis.Y();
			float z = nAxis.Z();

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

		public static FMat4 Translate(FVec3 translate)
		{
			FMat4 newMatrix = new FMat4(1.0f);

			newMatrix[0, 3] = translate.X();
			newMatrix[1, 3] = translate.Y();
			newMatrix[2, 3] = translate.Z();

			return newMatrix;
		}

		public static FMat4 Scale(FVec3 scale)
		{
			FMat4 newMatrix = new FMat4(1.0f);

			newMatrix[0, 0] = scale.X();
			newMatrix[1, 1] = scale.Y();
			newMatrix[2, 2] = scale.Z();
			newMatrix[3, 3] = 1;

			return newMatrix;
		}

		public static FMat4 Interpolate(FMat4 @from, FMat4 to, float factor)
		{
			float f = Functions.Clamp(factor, 0.0f, 1.0f);
			return ((1 - f) * from) + (f * to);
		}
	}
}
