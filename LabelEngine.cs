using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Label
{
    class LabelEngine
    {
        private class NameRef
        {
            public string Name { get; set; }
            public Hospital Hospital { get; set; }
        }

        public LabelEngine(string labelsFilePath)
        {
            Labels = Util.EnumerateAllLines(labelsFilePath).Select(entry => Hospital.Parse(entry)).ToList();
            Names = Labels
                .SelectMany(h => h.Names.Select(n => new NameRef { Name = n, Hospital = h }))
                .ToList();
            Names.Sort((n0, n1) => n0.Name.Length - n1.Name.Length);
        }

        // requires bi-directional all-name match
        public Hospital TryLabelV2(string entry)
        {
            var h = Hospital.Parse(entry);
            foreach (var candi in Labels)
            {
                if (AllNamesMatch(h.Names, candi.Names)
                    && SubnamesMatch(h.Subnames, candi.Subnames))
                {
                    return candi;
                }
            }
            return null;
        }

        public Hospital TryLabel(string entry)
        {
            var h = Hospital.Parse(entry);
            h.Names.Sort((n0, n1) => n1.Length - n0.Length);
            foreach (var name in h.Names)
            {
                var bestMatch = FindBestMatch(name, h, out int diff);
                if (bestMatch != null) return bestMatch;
            }
            return null;
        }

        private Hospital FindBestMatch(string name, Hospital h, out int diff)
        {
            Hospital match = null;
            diff = int.MaxValue;
            foreach (var nameRef in Names)
            {
                int d;
                if ((d = Math.Abs(name.Length - nameRef.Name.Length)) > diff) break;
                if (!SubnamesMatch(h.Subnames, nameRef.Hospital.Subnames)) continue;
                if (!NameMatch(name, nameRef.Name)) continue;
                // matched
                diff = d;
                match = nameRef.Hospital;
            }
            return match;
        }

        private static bool NameMatch(string a, string b)
        {
            string sub, main;
            if (a.Length <= b.Length)
            {
                sub = a;
                main = b;
            }
            else
            {
                sub = b;
                main = a;
            }
            return
                main.EndsWith(sub)
                && (main.Length - sub.Length != 1); // e.g., 沙县中医院 and 金沙县中医院
        }

        // bi-directional all-name match
        private static bool AllNamesMatch(List<string> namesA , List<string> namesB)
        {
            foreach (var name in namesA)
            {
                if (!namesB.Any(n => NameMatch(n, name))) return false;
            }
            foreach (var name in namesB)
            {
                if (!namesA.Any(n => NameMatch(n, name))) return false;
            }
            return true;
        }

        private static bool SubnamesMatch(string[] subnamesA, string[] subnamesB)
        {
            if (subnamesA.Length == 0 && subnamesB.Length == 0) return true;
            if (subnamesA.Length == 0 || subnamesB.Length == 0) return false;
            Trace.Assert(subnamesA.Length > 0 && subnamesB.Length > 0);
            foreach (var subname in subnamesA)
            {
                if (subnamesB.Any(s => s == subname)) return true;
            }
            return false;
        }

        private static int DiffNames(string a, string b)
        {
            if (a.Length > b.Length)
            {
                var temp = a;
                a = b;
                b = temp;
            }
            return b.EndsWith(a) ? (b.Length - a.Length) : int.MaxValue;
        }

        public List<Hospital> Labels { get; private set; }

        private List<NameRef> Names { get; set; }
    }
}
