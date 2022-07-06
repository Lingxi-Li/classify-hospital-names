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
            Labels = Util.EnumerateAllLines(labelsFilePath)
                .Select(entry => Hospital.Parse(entry))
                .OrderBy(h => h.NormalizedEntry, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Names = Labels
                .SelectMany(h => h.Names.Select(n => new NameRef { Name = n, Hospital = h }))
                .OrderBy(r => r.Name.Length)
                .ToList();
        }

        // DEPRECATED
        // requires bi-directional all-name match
        public Hospital TryLabelV2(string entry)
        {
            var h = Hospital.Parse(entry);
            foreach (var candi in Labels)
            {
                if (AllNamesMatch(h.Names, candi.Names)
                    && SubnamesMatch(h, candi))
                {
                    return candi;
                }
            }
            return null;
        }

        public Hospital TryLabel(string entry)
        {
            var h = Hospital.Parse(entry);
            var bestMatch = TryFindMatchByNormalizedEntry(h.NormalizedEntry);
            if (bestMatch != null) return bestMatch;

            if (h.NormalizedEntry.Length < Hospital.MinTitleLen) return null;
            foreach (var name in h.Names)
            {
                bestMatch = FindBestMatch(name, h, out int diff);
                if (bestMatch != null) return bestMatch;
            }
            return null;
        }

        private Hospital TryFindMatchByNormalizedEntry(string entry)
        {
            int left = 0, right = Labels.Count - 1;
            while (left <= right)
            {
                var mid = left + (right - left + 1) / 2;
                var res = StringComparer.OrdinalIgnoreCase.Compare(entry, Labels[mid].NormalizedEntry);
                if (res < 0) { right = mid - 1; continue; }
                if (res > 0) { left = mid + 1; continue; }
                return Labels[mid];
            }
            return null;
        }

        private Hospital FindBestMatch(string name, Hospital h, out int diff)
        {
            Hospital match = null;
            diff = int.MaxValue;
            var annoMatchCnt = 0;
            foreach (var nameRef in Names)
            {
                int d;
                if ((d = Math.Abs(name.Length - nameRef.Name.Length)) > diff) break;
                if (!SubnamesMatch(h, nameRef.Hospital)) continue;
                if (!NameMatch(name, nameRef.Name)) continue;
                // matched
                var newAnnoMatchCnt = AnnotationMatchCount(h, nameRef.Hospital);
                if (d < diff || newAnnoMatchCnt > annoMatchCnt)
                {
                    diff = d;
                    annoMatchCnt = newAnnoMatchCnt;
                    match = nameRef.Hospital;
                }
            }
            return match;
        }

        private static int AnnotationMatchCount(Hospital a, Hospital b)
        {
            return a.Annotations.Intersect(b.Annotations).Count();
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

        // bi-directional
        private static bool SubnamesMatch(Hospital a, Hospital b)
        {
            string[] subnamesA = a.Subnames, subnamesB = b.Subnames;
            if (subnamesA.Length == 0 && subnamesB.Length == 0) return true;
            foreach (var subname in subnamesA)
            {
                if (subnamesB.Any(s => s == subname)) return true;
                if (b.Names.Any(n => n.Contains(subname))) return true;
            }
            foreach (var subname in subnamesB)
            {
                if (a.Names.Any(n => n.Contains(subname))) return true;
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
