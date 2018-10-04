using System;
namespace StarlightEngine
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

        // modulo operator, instead of default '%' as remainder
        public static int Mod(int a, int b)
        {
            int r = a % b;
            return (r < 0) ? r + b : r;
        }
    }
}
