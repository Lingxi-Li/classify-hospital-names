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
            Run(namesPath, labelsPath, outputPath, GUI.Properties.DefaultThreadCount);
            if (useGui)
            {
                Console.Write("Press <Enter> to quit.");
                Console.ReadLine();
            }
        }

        static int Label(string[] names, LabelEngine labelEngine, int startIndex, int step, Hospital[] labels, bool logProgress)
        {
            var progress = 0;
            var matchCnt = 0;
            for (var i = startIndex; i < names.Length; i += step)
            {
                labels[i] = labelEngine.TryLabel(names[i]);
                if (labels[i] != null) ++matchCnt;
                if (logProgress)
                {
                    var currentProgress = (i * 100) / names.Length;
                    if (currentProgress >= progress + 10)
                    {
                        progress = currentProgress;
                        Console.WriteLine($"{progress}% processed");
                    }
                }
            }
            if (logProgress) Console.WriteLine("100% processed");
            return matchCnt;
        }

        static void Run(string namesPath, string labelsPath, string outputPath, int threadCnt)
        {
            var startTime = DateTime.Now;
            var names = File.ReadAllLines(namesPath);
            var labelEngine = new LabelEngine(labelsPath);
            var labels = new Hospital[names.Length];
            var tasks = new List<Task<int>>();
            for (var i = 0; i < threadCnt - 1; ++i)
            {
                var startIndex = i;
                tasks.Add(Task.Run(() => Label(names, labelEngine, startIndex, threadCnt, labels, false)));
            }
            var matchCnt = Label(names, labelEngine, threadCnt - 1, threadCnt, labels, true);
            matchCnt += tasks.Select(t => t.Result).Sum();
            Console.WriteLine();
            Console.Write($"Labeled {matchCnt.ToStr()} / {names.Length.ToStr()}");
            Console.WriteLine(names.Length > 0 ? $" ({Util.ToPercentStr(matchCnt, names.Length)})" : null);
            Console.WriteLine();
            Console.WriteLine("Writing result to output file...");
            using (var file = File.CreateText(outputPath))
            {
                for (var i = 0; i < names.Length; ++i)
                {
                    file.WriteLine($"{names[i]}\t{labels[i]?.OriginalEntry}");
                }
            }
            Console.WriteLine("All done.");
            Console.WriteLine();
            var duration = (DateTime.Now - startTime).ToString("c").Split('.')[0];
            Console.WriteLine($"Time elapsed: {duration}");
            Console.WriteLine();
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
            PrintAbout();
            Console.WriteLine();
        }
    }
}
