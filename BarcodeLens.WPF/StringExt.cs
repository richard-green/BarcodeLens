using System;
using System.Linq;

namespace BarcodeLens.WPF
{
    public static class StringExt
    {
        public static bool AnyOf(this string value, StringComparison comparison, params string[] values)
        {
            return values.Any(val => String.Equals(value, val, comparison));
        }
    }
}
