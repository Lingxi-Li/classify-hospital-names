using Label.Strategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        enum Mode
        {
            Invalid,
            Classification,
            Clustering
        }

        [STAThread]
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string namesPath, labelsPath, outputPath;
            var mode = ParseMode(args);
            bool useGui = mode == Mode.Invalid;
            int threadCnt = GUI.Properties.DefaultThreadCount;
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
                    threadCnt = gui.Inputs.Concurrency;
                    mode = string.IsNullOrWhiteSpace(labelsPath)
                        ? Mode.Clustering
                        : Mode.Classification;
                }
                else
                {
                    return;
                }
            }
            else
            {
                switch (mode)
                {
                    case Mode.Classification:
                        namesPath = args[0];
                        labelsPath = args[1];
                        outputPath = args[2];
                        break;
                    default:
                        namesPath = args[0];
                        labelsPath = null;
                        outputPath = args[1];
                        break;
                }
                //threadCnt = 1;
            }

            var startTime = DateTime.Now;
            if (mode == Mode.Classification)
            {
                Classify(namesPath, labelsPath, outputPath, threadCnt);
            }
            else
            {
                Cluster(namesPath, outputPath);
            }
            var duration = (DateTime.Now - startTime).ToString("c").Split('.')[0];
            Console.WriteLine($"Time elapsed: {duration}");
            Console.WriteLine();

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

        static void Classify(string namesPath, string labelsPath, string outputPath, int threadCnt)
        {
            var names = File.ReadAllLines(namesPath);
            var labelEngine = new LabelEngine(labelsPath);
            Console.WriteLine("Label engine initialized.");
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
        }

        static void Cluster(string namesPath, string outputPath)
        {
            var hospitals = Util.EnumerateAllLines(namesPath)
                .Where(ln => !string.IsNullOrWhiteSpace(ln))
                .Select(name => Hospital.Parse(name))
                .OrderBy(h => -h.NormalizedEntry.Length)
                .ToArray();
            using (var file = File.CreateText(outputPath))
            {
                var distinctHospitals = new List<Hospital>();
                var distinctHospitalNameSigs = new List<List<string[]>>();
                Console.WriteLine("0% processed");
                int cur = 0, progress = 0;
                foreach (var h in hospitals)
                {
                    var currentProgress = (cur++ * 100) / hospitals.Length;
                    if (currentProgress >= progress + 10)
                    {
                        progress = currentProgress;
                        Console.WriteLine($"{progress}% processed");
                    }

                    var matched = false;
                    for (var i = 0; i < distinctHospitals.Count; ++i)
                    {
                        var hh = distinctHospitals[i];
                        var hhsigs = distinctHospitalNameSigs[i];
                        if (!LabelEngine.SubnamesMatch(h, hh)) continue;
                        foreach (var nameA in h.Names)
                        {
                            for (var j = 0; j < hh.Names.Count; ++j)
                            {
                                matched = NameMatcher.Matches(nameA, hh.Names[j], hhsigs[j]);
                                if (matched) break;
                            }
                            if (matched) break;
                        }
                        if (matched) break;
                    }
                    if (matched) continue;

                    distinctHospitals.Add(h);
                    distinctHospitalNameSigs.Add(
                        h.Names
                        .Select(n => NameMatcher.ComputeNameSigs(n))
                        .ToList()
                    );
                    file.WriteLine(h.OriginalEntry);
                }
                Console.WriteLine("100% processed");
                Console.WriteLine();
                Console.WriteLine($"{hospitals.Length} => {distinctHospitals.Count} ({Util.ToPercentStr(distinctHospitals.Count, hospitals.Length)})");
                Console.WriteLine();
            }
        }

        static Mode ParseMode(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                PrintUsage();
                return Mode.Invalid;
            }
            var invalidPath = args.Take(args.Length - 1).FirstOrDefault(p => !File.Exists(p));
            if (invalidPath != null)
            {
                Console.WriteLine($"File not found: {invalidPath}");
                Console.WriteLine();
                PrintUsage();
                return Mode.Invalid;
            }
            return args.Length == 2 ? Mode.Clustering : Mode.Classification;
        }

        static void PrintAbout()
        {
            Console.WriteLine("About the tool: https://github.com/Lingxi-Li/classify-hospital-names");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Classification mode:");
            Console.WriteLine("  Usage: Label.exe \"<names>\" \"<labels>\" \"<output>\"");
            Console.WriteLine("  <names>: File path to external names");
            Console.WriteLine("  <labels>: File path to internal names");
            Console.WriteLine("  <output>: File path to output to");
            Console.WriteLine();
            Console.WriteLine("Clustering mode:");
            Console.WriteLine("  Usage: Label.exe \"<names>\" \"<output>\"");
            Console.WriteLine("  <names>: File path to names");
            Console.WriteLine("  <output>: File path to output to");
            Console.WriteLine();
            PrintAbout();
            Console.WriteLine();
        }
    }
}
