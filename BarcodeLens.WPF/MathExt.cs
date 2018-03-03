using System.Linq;

namespace BarcodeLens.WPF
{
    public static class MathExt
    {
        public static int Constrain(int value, int minValue, int maxValue)
        {
            if (value > maxValue) return maxValue;
            if (value < minValue) return minValue;
            return value;
        }

        public static float Constrain(float value, float minValue, float maxValue)
        {
            if (value > maxValue) return maxValue;
            if (value < minValue) return minValue;
            return value;
        }

        public static double Constrain(double value, double minValue, double maxValue)
        {
            if (value > maxValue) return maxValue;
            if (value < minValue) return minValue;
            return value;
        }

        public static int Smallest(params int[] values)
        {
            return values.OrderBy(v => v).First();
        }

        public static float Smallest(params float[] values)
        {
            return values.OrderBy(v => v).First();
        }

        public static double Smallest(params double[] values)
        {
            return values.OrderBy(v => v).First();
        }

        public static int Largest(params int[] values)
        {
            return values.OrderByDescending(v => v).First();
        }

        public static float Largest(params float[] values)
        {
            return values.OrderByDescending(v => v).First();
        }

        public static double Largest(params double[] values)
        {
            return values.OrderByDescending(v => v).First();
        }
    }
}
