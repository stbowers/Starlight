using System;

namespace StarlightEngine.Math
{
    public static class RNG
    {
        public static Random m_rng;

        static RNG()
        {
            m_rng = new Random();
        }

        public static void SeedRNG(int seed)
        {
            m_rng = new Random(seed);
        }

        public static Random GetRNG()
        {
            return m_rng;
        }
    }
}