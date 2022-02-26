using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Label
{
    static class Util
    {
        public static IEnumerable<string> EnumerateAllLines(string filePath)
        {
            using (var file = File.OpenText(filePath))
            {
                string line;
                while ((line = file.ReadLine()) != null) yield return line;
            }
        }

        public static string ToStr(this int value)
        {
            return value.ToString("N0");
        }

        public static string ToPercentStr(this double value)
        {
            return $"{value * 100:0.##}%";
        }

        public static string ToPercentStr(int a, int b)
        {
            return ((double)a / b).ToPercentStr();
        }
    }
}
