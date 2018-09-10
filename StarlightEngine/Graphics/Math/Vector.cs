using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarlightEngine.Graphics.Math
{
    public class BasicVector<T>: BasicMatrix<T>
    {
        public BasicVector(int m): base(1, m)
        {
        }

        public BasicVector(int m, T[] data): base(1, m, data)
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
    }

    public class Vec2 : BasicVector<float>
    {
        public Vec2() : base(2)
        {
        }

        public Vec2(float[] data): base(2, data)
        {
        }

        public Vec2(float x, float y) : this()
        {
            this[0] = x;
            this[1] = y;
        }

        public float X
        {
            get
            {
                return this[0];
            }
            set
            {
                this[0] = value;
            }
        }

        public float Y
        {
            get
            {
                return this[1];
            }
            set
            {
                this[1] = value;
            }
        }
    }

    public class Vec3 : BasicVector<float>
    {
        public Vec3(): base(3)
        {
        }

        public Vec3(float[] data): base(3, data)
        {
        }

        public Vec3(float x, float y, float z): this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
        }

        public float X
        {
            get
            {
                return this[0];
            }
            set
            {
                this[0] = value;
            }
        }

        public float Y
        {
            get
            {
                return this[1];
            }
            set
            {
                this[1] = value;
            }
        }

        public float Z
        {
            get
            {
                return this[2];
            }
            set
            {
                this[2] = value;
            }
        }

        // Math operations
        public static Vec3 operator-(Vec3 left, Vec3 right)
        {
            return new Vec3(left[0] - right[0], left[1] - right[1], left[2] - right[2]);
        }

        public Vec3 Cross(Vec3 vector)
        {
            Vec3 product = new Vec3();

            product.X = (this.Y * vector.Z) - (this.Z * vector.Y);
            product.Y = (this.Z * vector.X) - (this.X * vector.Z);
            product.Z = (this.X * vector.Y) - (this.Y * vector.X);

            return product;
        }

        public float Dot(Vec3 vector)
        {
            return (this.X * vector.X) + (this.Y * vector.Y) + (this.Z * vector.Z);
        }

        public void Normalize()
        {
            float length = (float)System.Math.Sqrt(System.Math.Pow(this.X, 2) + System.Math.Pow(this.Y, 2) + System.Math.Pow(this.Z, 2));
            this.X /= length;
            this.Y /= length;
            this.Z /= length;
        }
    }

    public class Vec4: BasicVector<float>
    {
        public Vec4(): base(4)
        {

        }

        public Vec4(float x, float y, float z, float w): this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
            this[3] = w;
        }
    }

	public class IVec2 : BasicVector<int>
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

	public class IVec3 : BasicVector<int>
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

		public override bool Equals(object obj)
		{
			IVec3 vec = obj as IVec3;
			return (this[0] == vec[0]) &&
				(this[1] == vec[1]) &&
				(this[2] == vec[2]);
		}
    }

	public class IVec4 : BasicVector<int>
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
