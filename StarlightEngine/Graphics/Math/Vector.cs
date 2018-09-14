using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarlightEngine.Graphics.Math
{
    public class BasicVector<T>: BasicMatrix<T>
    {
		public BasicVector(int m, SimpleOperations<T> operations): base(1, m, operations)
        {
        }

		public BasicVector(int m, T[] data, SimpleOperations<T> operations): base(1, m, data, operations)
        {
        }

        public T this[int n]
        {
            get
            {
                return this[n, 0];
            }

            set
            {
                this[n, 0] = value;
            }
        }

		// Basic accessor methods; these should be 'deleted' by child classes that don't need them, i.e:
		/* [Obsolete("...", false)]
		 * public new float W{ get{ throw new NotSupportedException(); } }
		 */

		public T X
		{
			get
			{
				return m_data[0];
			}
			set
			{
				m_data[0] = value;
			}
		}

		public T Y
		{
			get
			{
				return m_data[1];
			}
			set
			{
				m_data[1] = value;
			}
		}

		public T Z
		{
			get
			{
				return m_data[2];
			}
			set
			{
				m_data[2] = value;
			}
		}

		public T W
		{
			get
			{
				return m_data[3];
			}
			set
			{
				m_data[3] = value;
			}
		}

		// Vector operations
		public T Length()
		{
			T length = m_operations.zero();
			foreach (T element in m_data)
			{
				length = m_operations.add(length, m_operations.pow(element, 2));
			}
			length = m_operations.pow(length, 0.5f);

			return length;
		}

		public void Normalize()
		{
			for (int i = 0; i < m_data.Length; i++)
			{
				m_data[i] = m_operations.divide(m_data[i], Length());
			}
		}

		public T Dot(BasicVector<T> other)
		{
			if (this.m_data.Length != other.m_data.Length)
			{
				throw new InvalidOperationException();
			}

			T product = m_operations.zero();
			for (int i = 0; i < m_data.Length; i++)
			{
				T componentProduct = m_operations.multiply(this[i], other[i]);
				product = m_operations.add(product, componentProduct);
			}

			return product;
		}
	}

	// base for float vectors
	public class FVec : BasicVector<float>
	{
		public FVec(int m) : base(m, FloatOperations.Operations)
		{
		}

		public FVec(int m, float[] data) : base(m, data, FloatOperations.Operations)
		{
		}

		public FVec(int m, BasicMatrix<float> copyFrom) : base(m, copyFrom.Data, FloatOperations.Operations)
		{
		}
	}

    public class FVec2 : FVec
    {
        public FVec2() : base(2)
        {
        }

        public FVec2(float[] data): base(2, data)
        {
        }

		public FVec2(BasicMatrix<float> copyFrom) : base(2, copyFrom)
		{
		}

		public FVec2(float x, float y) : this()
        {
            this[0] = x;
            this[1] = y;
        }

		[Obsolete("Z is not a valid member of FVec2", true)]
		public new float Z { get { throw new NotSupportedException(); } }
		[Obsolete("W is not a valid member of FVec2", true)]
		public new float W{ get{ throw new NotSupportedException(); } }
    }

    public class FVec3 : FVec
    {
        public FVec3(): base(3)
        {
        }

        public FVec3(float[] data): base(3, data)
        {
        }

		public FVec3(BasicMatrix<float> copyFrom) : base(3, copyFrom)
		{
		}

        public FVec3(float x, float y, float z): this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
        }

		[Obsolete("W is not a valid member of FVec3", true)]
		public new float W { get { throw new NotSupportedException(); } }

        // Math operations
        public FVec3 Cross(FVec3 vector)
        {
            FVec3 product = new FVec3();

            product.X = (this.Y * vector.Z) - (this.Z * vector.Y);
            product.Y = (this.Z * vector.X) - (this.X * vector.Z);
            product.Z = (this.X * vector.Y) - (this.Y * vector.X);

            return product;
        }

		public static FVec3 operator +(FVec3 left, FVec3 right)
		{
			BasicMatrix<float> l = left as BasicMatrix<float>;
			BasicMatrix<float> r = right as BasicMatrix<float>;
			BasicMatrix<float> sum = l + r;
			return new FVec3(sum);
		}

		public static FVec3 operator -(FVec3 left, FVec3 right)
		{
			BasicMatrix<float> l = left as BasicMatrix<float>;
			BasicMatrix<float> r = right as BasicMatrix<float>;
			BasicMatrix<float> difference = l - r;
			return new FVec3(difference);
		}
    }

    public class FVec4: FVec
    {
        public FVec4(): base(4)
        {

        }

		public FVec4(float[] data): base(4, data)
		{
		}

        public FVec4(float x, float y, float z, float w): this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
            this[3] = w;
        }
    }

	// base for int vectors
	public class IVec : BasicVector<int>
	{
		public IVec(int m) : base(m, IntOperations.Operations)
		{
		}

		public IVec(int m, int[] data) : base(m, data, IntOperations.Operations)
		{
		}

		public IVec(int m, BasicVector<int> copyFrom) : base(m, copyFrom.Data, IntOperations.Operations)
		{
		}
	}

	public class IVec2 : IVec
	{
		public IVec2() : base(2)
		{
		}

		public IVec2(int[] data) : base(2, data)
		{
		}

		public IVec2(int x, int y) : this()
		{
			this[0] = x;
			this[1] = y;
		}
	}

	public class IVec3 : IVec
    {
        public IVec3(): base(3)
        {
        }

        public IVec3(int[] data): base(3, data)
        {
        }

        public IVec3(int x, int y, int z): this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
        }
    }

	public class IVec4 : IVec
	{
		public IVec4() : base(4)
		{
		}

		public IVec4(int[] data) : base(4, data)
		{
		}

		public IVec4(int x, int y, int z, int w) : this()
		{
			this[0] = x;
			this[1] = y;
			this[2] = z;
			this[3] = w;
		}
	}
}
