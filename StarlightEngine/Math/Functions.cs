using System;
namespace StarlightEngine.Math
{
    public static class Functions
    {
        public static float Clamp(float @value, float min, float max)
        {
            if (value > max)
            {
                return max;
            }
            if (value < min)
            {
                return min;
            }
            return value;
        }

        public static float Smoothstep(float edge0, float edge1, float x)
        {
            x = Clamp((x - edge0) / (edge1 - x), 0.0f, 1.0f);
            return x * x * (3 - (2 * x));
        }

        // modulo operator, instead of default '%' as remainder
        public static int Mod(int a, int b)
        {
            int r = a % b;
            return (r < 0) ? r + b : r;
        }
    }
}
