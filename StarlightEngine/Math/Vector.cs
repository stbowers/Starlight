using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarlightEngine.Math
{
    public class BasicVector<T> : BasicMatrix<T>
    {
        public BasicVector(int m, SimpleOperations<T> operations) : base(1, m, operations)
        {
        }

        public BasicVector(int m, T[] data, SimpleOperations<T> operations) : base(1, m, data, operations)
        {
        }

        public BasicVector(int m, byte[] data, SimpleOperations<T> operations) : base(1, m, data, operations)
        { }

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

        // Accessors
        public int Dimension
        {
            get
            {
                return m_m;
            }
        }

        public SimpleOperations<T> SimpleOperations
        {
            get
            {
                return m_operations;
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

        public FVec(int m, byte[] data) : base(m, data, FloatOperations.Operations)
        { }

        public FVec(int m, BasicMatrix<float> copyFrom) : base(m, copyFrom.Data, FloatOperations.Operations)
        {
        }
    }

    public class FVec2 : FVec
    {
        public FVec2() : base(2)
        {
        }

        public FVec2(float[] data) : base(2, data)
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

        public static FVec2 operator +(FVec2 left, FVec2 right)
        {
            BasicMatrix<float> l = left as BasicMatrix<float>;
            BasicMatrix<float> r = right as BasicMatrix<float>;
            BasicMatrix<float> sum = l + r;
            return new FVec2(sum);
        }

        public static FVec2 operator -(FVec2 left, FVec2 right)
        {
            BasicMatrix<float> l = left as BasicMatrix<float>;
            BasicMatrix<float> r = right as BasicMatrix<float>;
            BasicMatrix<float> difference = l - r;
            return new FVec2(difference);
        }
    }

    public class FVec3 : FVec
    {
        // const values for common vectors
        public static readonly FVec3 Zero = new FVec3();
        public static readonly FVec3 I = new FVec3(1.0f, 0.0f, 0.0f);
        public static readonly FVec3 J = new FVec3(0.0f, 1.0f, 0.0f);
        public static readonly FVec3 K = new FVec3(0.0f, 0.0f, 1.0f);
        public static readonly FVec3 Up = J;
        public static readonly FVec3 Down = -1.0f * J;
        public static readonly FVec3 Left = -1.0f * I;
        public static readonly FVec3 Right = I;
        public static readonly FVec3 Forward = K;
        public static readonly FVec3 Backward = -1.0f * K;

        public FVec3() : base(3)
        {
        }

        public FVec3(float[] data) : base(3, data)
        {
        }

        public FVec3(BasicMatrix<float> copyFrom) : base(3, copyFrom)
        {
        }

        public FVec3(float x, float y, float z) : this()
        {
            this[0] = x;
            this[1] = y;
            this[2] = z;
        }

        // Math operations
        public FVec3 Cross(FVec3 vector)
        {
            FVec3 product = new FVec3();

            product.SetX((this.Y() * vector.Z()) - (this.Z() * vector.Y()));
            product.SetY((this.Z() * vector.X()) - (this.X() * vector.Z()));
            product.SetZ((this.X() * vector.Y()) - (this.Y() * vector.X()));

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

        public static FVec3 operator *(float scalar, FVec3 vector)
        {
            return new FVec3(scalar * vector.X(), scalar * vector.Y(), scalar * vector.Z());
        }
    }

    public class FVec4 : FVec
    {
        public FVec4() : base(4)
        {

        }

        public FVec4(float[] data) : base(4, data)
        {
        }

        public FVec4(byte[] data) : base(4, data)
        {
        }

        public FVec4(float x, float y, float z, float w) : this()
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
        public IVec3() : base(3)
        {
        }

        public IVec3(int[] data) : base(3, data)
        {
        }

        public IVec3(int x, int y, int z) : this()
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

    /// <summary>
    /// Swizzle extensions for vectors
    /// </summary>
    public static class SwizzleExtensions
    {
        #region Generic Swizzles
        // 1D
        private delegate U Swizzle1D<T, U>(T vector) where T : BasicVector<U>;
        private static U X<T, U>(this T vector) where T : BasicVector<U> { return vector[0]; }
        private static U Y<T, U>(this T vector) where T : BasicVector<U> { return vector[1]; }
        private static U Z<T, U>(this T vector) where T : BasicVector<U> { return vector[2]; }
        private static U W<T, U>(this T vector) where T : BasicVector<U> { return vector[3]; }
        private static void SetX<T, U>(this T vector, U value) where T : BasicVector<U> { vector[0] = value; }
        private static void SetY<T, U>(this T vector, U value) where T : BasicVector<U> { vector[1] = value; }
        private static void SetZ<T, U>(this T vector, U value) where T : BasicVector<U> { vector[2] = value; }
        private static void SetW<T, U>(this T vector, U value) where T : BasicVector<U> { vector[3] = value; }

        // 2D
        private static R Swizzle2D<T, U, R>(this T vector, Swizzle1D<T, U> x, Swizzle1D<T, U> y)
        where T : BasicVector<U>
        where R : BasicVector<U>, new()
        {
            R newVector = new R();

            newVector.SetX(x(vector));
            newVector.SetY(y(vector));

            return newVector;
        }

        // 3D
        private static BasicVector<U> Swizzle3D<T, U, R>(this T vector, Swizzle1D<T, U> x, Swizzle1D<T, U> y, Swizzle1D<T, U> z)
        where T : BasicVector<U>
        where R : BasicVector<U>, new()
        {
            R newVector = new R();

            newVector.SetX(x(vector));
            newVector.SetY(y(vector));
            newVector.SetZ(z(vector));

            return newVector;
        }

        // 4D
        private static BasicVector<U> Swizzle4D<T, U, R>(this T vector, Swizzle1D<T, U> x, Swizzle1D<T, U> y, Swizzle1D<T, U> z, Swizzle1D<T, U> w)
        where T : BasicVector<U>
        where R : BasicVector<U>, new()
        {
            R newVector = new R();

            newVector.SetX(x(vector));
            newVector.SetY(y(vector));
            newVector.SetZ(z(vector));
            newVector.SetW(w(vector));

            return newVector;
        }
        #endregion

        #region FVec2 Swizzles
        public static float X(this FVec2 vector) { return vector.X<FVec2, float>(); }
        public static float Y(this FVec2 vector) { return vector.Y<FVec2, float>(); }
        public static void SetX(this FVec2 vector, float value) { vector.SetX<FVec2, float>(value); }
        public static void SetY(this FVec2 vector, float value) { vector.SetY<FVec2, float>(value); }

        public static FVec2 XY(this FVec2 vector) { return (FVec2)Swizzle2D<FVec2, float, FVec2>(vector, X, Y); }
        public static FVec2 YX(this FVec2 vector) { return (FVec2)Swizzle2D<FVec2, float, FVec2>(vector, Y, X); }
        #endregion

        #region FVec3 Swizzles
        public static float X(this FVec3 vector) { return vector.X<FVec3, float>(); }
        public static float Y(this FVec3 vector) { return vector.Y<FVec3, float>(); }
        public static float Z(this FVec3 vector) { return vector.Z<FVec3, float>(); }
        public static void SetX(this FVec3 vector, float value) { vector.SetX<FVec3, float>(value); }
        public static void SetY(this FVec3 vector, float value) { vector.SetY<FVec3, float>(value); }
        public static void SetZ(this FVec3 vector, float value) { vector.SetZ<FVec3, float>(value); }

        public static FVec2 XY(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, X, Y); }
        public static FVec2 YX(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, Y, X); }
        public static FVec2 XZ(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, X, Z); }
        public static FVec2 ZX(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, Z, X); }
        public static FVec2 YZ(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, Y, Z); }
        public static FVec2 ZY(this FVec3 vector) { return (FVec2)Swizzle2D<FVec3, float, FVec2>(vector, Z, Y); }

        public static FVec3 XYZ(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, X, Y, Z); }
        public static FVec3 YXZ(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, Y, X, Z); }
        public static FVec3 XZY(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, X, Z, Y); }
        public static FVec3 YZX(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, Y, Z, X); }
        public static FVec3 ZXY(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, Z, X, Y); }
        public static FVec3 ZYX(this FVec3 vector) { return (FVec3)Swizzle3D<FVec3, float, FVec3>(vector, Z, Y, X); }
        #endregion

        #region FVec4 Swizzles
        public static float X(this FVec4 vector) { return vector.X<FVec4, float>(); }
        public static float Y(this FVec4 vector) { return vector.Y<FVec4, float>(); }
        public static float Z(this FVec4 vector) { return vector.Z<FVec4, float>(); }
        public static float W(this FVec4 vector) { return vector.W<FVec4, float>(); }
        public static void SetX(this FVec4 vector, float value) { vector.SetX<FVec4, float>(value); }
        public static void SetY(this FVec4 vector, float value) { vector.SetY<FVec4, float>(value); }
        public static void SetZ(this FVec4 vector, float value) { vector.SetZ<FVec4, float>(value); }
        public static void SetW(this FVec4 vector, float value) { vector.SetW<FVec4, float>(value); }

        public static FVec2 XY(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, X, Y); }
        public static FVec2 YX(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Y, X); }
        public static FVec2 XZ(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, X, Z); }
        public static FVec2 ZX(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Z, X); }
        public static FVec2 YZ(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Y, Z); }
        public static FVec2 ZY(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Z, Y); }
        public static FVec2 XW(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, X, W); }
        public static FVec2 WX(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, W, X); }
        public static FVec2 WZ(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, W, Z); }
        public static FVec2 ZW(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Z, W); }
        public static FVec2 YW(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, Y, W); }
        public static FVec2 WY(this FVec4 vector) { return (FVec2)Swizzle2D<FVec4, float, FVec2>(vector, W, Y); }

        public static FVec3 XYZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, X, Y, Z); }
        public static FVec3 YXZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Y, X, Z); }
        public static FVec3 XZY(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, X, Z, Y); }
        public static FVec3 YZX(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Y, Z, X); }
        public static FVec3 ZXY(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, X, Y); }
        public static FVec3 ZYX(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, Y, X); }
        public static FVec3 XWZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, X, W, Z); }
        public static FVec3 WXZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, W, X, Z); }
        public static FVec3 XZW(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, X, Z, W); }
        public static FVec3 WZX(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, W, Z, X); }
        public static FVec3 ZXW(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, X, W); }
        public static FVec3 ZWX(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, W, X); }
        public static FVec3 YWZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Y, W, Z); }
        public static FVec3 WYZ(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, W, Y, Z); }
        public static FVec3 YZW(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Y, Z, W); }
        public static FVec3 WZY(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, W, Z, Y); }
        public static FVec3 ZYW(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, Y, W); }
        public static FVec3 ZWY(this FVec4 vector) { return (FVec3)Swizzle3D<FVec4, float, FVec3>(vector, Z, W, Y); }

        #endregion
    }
}
