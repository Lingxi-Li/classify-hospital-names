using Label.Strategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Label
{
    public class LabelEngine
    {
        private class NameRef
        {
            public string Name { get; set; }
            public Hospital Hospital { get; set; }
            public string[] NameSigs { get; set; }
        }

        public LabelEngine(string labelsFilePath)
            : this(Util.EnumerateAllLines(labelsFilePath))
        {}

        public LabelEngine(IEnumerable<string> entries)
        {
            Labels = entries
                .Where(ln => !string.IsNullOrWhiteSpace(ln))
                .Select(entry => Hospital.Parse(entry))
                .OrderBy(h => h.NormalizedEntry, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Names = Labels
                .SelectMany(h => h.Names.Select(n => new NameRef { Name = n, Hospital = h }))
                .OrderBy(r => r.Name.Length)
                .ToList();
            foreach (var name in Names)
            {
                name.NameSigs = NameMatcher.ComputeNameSigs(name.Name);
            }
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
                if (!NameMatcher.Matches(name, nameRef.Name, nameRef.NameSigs)) continue;
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

        // bi-directional
        private static bool SubnamesMatch(Hospital a, Hospital b)
        {
            return ContainsAnySubname(a, b.Subnames)
                && ContainsAnySubname(b, a.Subnames);
        }

        private static bool ContainsAnySubname(Hospital h, string[] subnames)
        {
            if (subnames.Length == 0) return true;
            if (h.Subnames.Intersect(subnames).Any()) return true;
            return h.Names.Any(
                n => subnames.Any(sn => n.Contains(sn))
            );
        }

        public List<Hospital> Labels { get; private set; }

        private List<NameRef> Names { get; set; }
    }
}
