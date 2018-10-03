using System;

namespace StarlightEngine.Graphics.Math
{
	/* A quaternion is a specialized Vec4 representing 4D complex space, often used for rotations
	 */
	public class Quaternion: FVec4
	{
		public float R
		{
			get
			{
				return this.X();
			}
			set
			{
				this.SetX(value);
			}
		}
		public float I
		{
			get
			{
				return this.Y();
			}
			set
			{
				this.SetY(value);
			}
		}
		public float J
		{
			get
			{
				return this.Z();
			}
			set
			{
				this.SetZ(value);
			}
		}
		public float K
		{
			get
			{
				return this.W();
			}
			set
			{
				this.SetW(value);
			}
		}

		public static Quaternion operator *(float scalar, Quaternion quaternion)
		{
			Quaternion product = new Quaternion();

			product.R = scalar * quaternion.R;
			product.I = scalar * quaternion.I;
			product.J = scalar * quaternion.J;
			product.K = scalar * quaternion.K;

			return product;
		}

		public static Quaternion operator +(Quaternion left, Quaternion right)
		{
			Quaternion sum = new Quaternion();

			sum.R = left.R + right.R;
			sum.I = left.I + right.I;
			sum.J = left.J + right.J;
			sum.K = left.K + right.K;

			return sum;
		}

		public FMat4 GetRotationMatrix()
		{
			FMat4 result = new FMat4();

			float s = (float)System.Math.Pow(Length(), -2.0f);

			result[0, 0] = 1 - (2 * s * ((J * J) + (K * K)));
			result[0, 1] =     (2 * s * ((I * J) - (K * R)));
			result[0, 2] =     (2 * s * ((I * K) + (J * R)));
			result[0, 3] = 0;
			result[1, 0] =     (2 * s * ((I * J) + (K * R)));
			result[1, 1] = 1 - (2 * s * ((I * I) + (K * K)));
			result[1, 2] =     (2 * s * ((J * K) - (I * R)));
			result[1, 3] = 0;
			result[2, 0] =     (2 * s * ((I * K) - (J * R)));
			result[2, 1] =     (2 * s * ((J * K) + (I * R)));
			result[2, 2] = 1 - (2 * s * ((I * I) + (J * J)));
			result[2, 3] = 0;
			result[3, 0] = 0;
			result[3, 1] = 0;
			result[3, 2] = 0;
			result[3, 3] = 1;

			return result;
		}

		public static Quaternion Rotate(float angle, FVec3 axis)
		{
			Quaternion rotation = new Quaternion();

			rotation.R = (float)System.Math.Cos(angle / 2);
			rotation.I = (float)System.Math.Sin(angle / 2) * axis.X();
			rotation.J = (float)System.Math.Sin(angle / 2) * axis.Y();
			rotation.K = (float)System.Math.Sin(angle / 2) * axis.Z();

			return rotation;
		}

		public static Quaternion Lerp(Quaternion from, Quaternion to, float factor)
		{
			float f = Functions.Clamp(factor, 0.0f, 1.0f);
			return ((1 - f) * from) + (f * to);
		}

		public static Quaternion Slerp(Quaternion from, Quaternion to, float factor)
		{
			float f = Functions.Clamp(factor, 0.0f, 1.0f);
			float omega = (float)System.Math.Acos(from.Dot(to));
			float c1 = ((float)System.Math.Sin((1 - f) * omega)) / ((float)System.Math.Sin(omega));
			float c2 = ((float)System.Math.Sin(f * omega)) / ((float)System.Math.Sin(omega));
			return (c1 * from) + (c2 * to);
		}
	}
}
