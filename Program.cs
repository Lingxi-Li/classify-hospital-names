using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Label
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string namesPath, labelsPath, outputPath;
            bool useGui = !Valid(args);
            if (useGui)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var gui = new GUI();
                if (gui.ShowDialog() == DialogResult.OK)
                {
                    namesPath = gui.Inputs.NamesFilePath;
                    labelsPath = gui.Inputs.LabelsFilePath;
                    outputPath = gui.Inputs.OutputFilePath;
                }
                else
                {
                    return;
                }
            }
            else
            {
                namesPath = args[0];
                labelsPath = args[1];
                outputPath = args[2];
            }
            Run(namesPath, labelsPath, outputPath, GUI.Properties.ThreadCnt);
            if (useGui)
            {
                Console.Write("Press <Enter> to quit.");
                Console.ReadLine();
            }
        }

        static void Run(string namesPath, string labelsPath, string outputPath, int threadCnt)
        {
            var startTime = DateTime.Now;
            using (var file = File.CreateText(outputPath))
            {
                var labelEngine = new LabelEngine(labelsPath);
                int cnt = 0, labeled = 0;
                foreach (var entry in Util.EnumerateAllLines(namesPath))
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
                var duration = (DateTime.Now - startTime).ToString("c").Split('.')[0];
                Console.WriteLine($"Time elapsed: {duration}");
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
