using System;

namespace StarlightGame.GameCore.Field.Galaxy
{
    public enum Shape
    {
        Rectangular,
        Eliptical,
        Spiral2,
        Spiral3
    }

    public static class ShapeExtensions
    {
        public static readonly float[,] rectangularBiases = {
            {0.7f, 0.7f, 0.7f, 0.7f},
            {0.7f, 0.7f, 0.7f, 0.7f},
            {0.7f, 0.7f, 0.7f, 0.7f},
            {0.7f, 0.7f, 0.7f, 0.7f},
        };

        public static readonly float[,] spiral2BiasesQ1 = {
            {0.1f, 0.3f, 0.7f, 0.3f},
            {0.3f, 0.7f, 1.0f, 0.7f},
            {0.7f, 1.0f, 0.7f, 1.0f},
            {1.0f, 0.7f, 0.1f, 1.0f},
        };
        public static readonly float[,] spiral2BiasesQ2 = {
            {0.1f, 0.3f, 0.7f, 1.0f},
            {0.3f, 0.7f, 1.0f, 0.7f},
            {0.7f, 1.0f, 0.7f, 0.3f},
            {1.0f, 0.7f, 0.3f, 0.1f},
        };
        public static readonly float[,] spiral2BiasesQ3 = {
            {1.0f, 0.1f, 0.7f, 1.0f},
            {1.0f, 0.7f, 1.0f, 0.7f},
            {0.7f, 1.0f, 0.7f, 0.3f},
            {0.3f, 0.7f, 0.3f, 0.1f},
        };
        public static readonly float[,] spiral2BiasesQ4 = {
            {0.1f, 0.3f, 0.7f, 1.0f},
            {0.3f, 0.7f, 1.0f, 0.7f},
            {0.7f, 1.0f, 0.7f, 0.3f},
            {1.0f, 0.7f, 0.3f, 0.1f},
        };

        // get a 2d array of biases for the given quadrant, given the galaxy shape,
        // which tell how likely it is for a star to spawn in any specific place
        public static float[,] GetGenerationBiases(this Shape shape, int quadrant)
        {
            switch (shape)
            {
                case Shape.Rectangular:
                    return rectangularBiases;
                case Shape.Spiral2:
                    switch (quadrant){
                        case 0:
                            return spiral2BiasesQ1;
                        case 1:
                            return spiral2BiasesQ2;
                        case 2:
                            return spiral2BiasesQ3;
                        case 3:
                            return spiral2BiasesQ4;
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }
    }
}