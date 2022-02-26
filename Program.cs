using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Label
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Valid(args)) return;

            Console.OutputEncoding = Encoding.UTF8;
            using (var file = File.CreateText(args[2]))
            {
                var labelEngine = new LabelEngine(args[1]);
                int cnt = 0, labeled = 0;
                foreach (var entry in Util.EnumerateAllLines(args[0]))
                {
                    Console.WriteLine($"{(++cnt).ToStr()}: {entry}");
                    var h = labelEngine.TryLabel(entry);
                    if (h != null) ++labeled;
                    file.WriteLine($"{entry}\t{h?.OriginalEntry}");
                }
                Console.WriteLine();
                Console.Write($"Labeled {labeled.ToStr()} / {cnt.ToStr()}");
                Console.WriteLine(cnt > 0 ? $" ({Util.ToPercentStr(labeled, cnt)})" : null);
                Console.WriteLine();
                PrintAbout();
            }
        }

        static bool Valid(string[] args)
        {
            if (args.Length != 3)
            {
                PrintUsage();
                return false;
            }
            var invalidPath = args.Take(2).FirstOrDefault(p => !File.Exists(p));
            if (invalidPath != null)
            {
                Console.WriteLine($"File not found: {invalidPath}");
                Console.WriteLine();
                PrintUsage();
                return false;
            }
            return true;
        }

        static void PrintAbout()
        {
            Console.WriteLine("About the tool: https://github.com/Lingxi-Li/classify-hospital-names");
        }

        static void PrintUsage()
        {
            Console.WriteLine($"Usage: Label.exe \"<names>\" \"<labels>\" \"<output>\"");
            Console.WriteLine("<names>: File path to external names");
            Console.WriteLine("<labels>: File path to internal names");
            Console.WriteLine("<output>: File path to output to");
            Console.WriteLine();
            Console.WriteLine("For correct display, use a Command Prompt font that supports Chinese characters.");
            Console.WriteLine();
            PrintAbout();
        }
    }
}
