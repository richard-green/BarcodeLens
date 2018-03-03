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
    }
}
