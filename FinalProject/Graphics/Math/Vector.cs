using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject.Graphics.Math
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
                return this[0, n];
            }

            set
            {
                this[0, n] = value;
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
    }
}
