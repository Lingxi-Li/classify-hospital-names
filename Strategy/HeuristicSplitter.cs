using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Label.Strategy
{
    public static class HeuristicSplitter
    {
        private static Regex MainTitleRegex = new Regex("医院");

        public static List<string> Split(string str)
        {
            foreach (var subEndTag in Hospital.SubEndTags)
            {
                str = str.Replace($"医院{subEndTag}", subEndTag);
            }
            var names = new List<string>();
            var regexstr = $"({string.Join("|", Hospital.SubEndTags)})";
            var regex = new Regex(regexstr);
            var pos = 0;
            foreach (Match match in regex.Matches(str))
            {
                var end = match.Index + 2;
                var substr = str.Substring(pos, end - pos);
                var ns = SplitSubEndTag(substr);
                names.AddRange(ns);
                pos = end;
            }
            if (pos < str.Length)
            {
                var substr = str.Substring(pos);
                var ns = SplitNoSubEndTag(substr);
                names.AddRange(ns);
            }
            return names;
        }

        private static List<string> SplitSubEndTag(string str)
        {
            var names = new List<string>();
            var matches = MainTitleRegex.Matches(str);
            string mainTitle, subTitle;
            Match m, mm;
            switch (matches.Count)
            {
                case 0:
                    names.Add(str);
                    break;
                case 1:
                    m = matches[0];
                    mainTitle = str.Substring(0, m.Index + 2);
                    subTitle = str.Substring(m.Index + 2);
                    names.Add($"{mainTitle}{Hospital.SubnameDelimiter}{subTitle}");
                    break;
                default: // >= 2
                    m = matches[matches.Count - 1];
                    mm = matches[matches.Count - 2];
                    mainTitle = str.Substring(mm.Index + 2, m.Index - mm.Index);
                    subTitle = str.Substring(m.Index + 2);
                    names.Add($"{mainTitle}{Hospital.SubnameDelimiter}{subTitle}");
                    var substr = str.Substring(0, mm.Index + 2);
                    var ns = SplitNoSubEndTag(substr);
                    names.AddRange(ns);
                    break;
            }
            return names;
        }

        private static List<string> SplitNoSubEndTag(string str)
        {
            var names = new List<string>();
            var matches = MainTitleRegex.Matches(str);
            var pos = 0;
            foreach (Match match in matches)
            {
                var end = match.Index + 2;
                var substr = str.Substring(pos, end - pos);
                UpdateOrAppend(names, substr);
                pos = end;
            }
            if (pos < str.Length)
            {
                var substr = str.Substring(pos);
                UpdateOrAppend(names, substr);
            }
            return names;
        }

        private static void UpdateOrAppend(List<string> names, string name)
        {
            if (names.Count == 0)
            {
                names.Add(name);
            }
            else
            {
                var last = names.Count - 1;
                if (names[last].Contains(Hospital.SubnameDelimiter)
                    || !IsSubname(name))
                {
                    names.Add(name);
                }
                else
                {
                    names[last] = $"{names[last]}{Hospital.SubnameDelimiter}{name}";
                }
            }
        }

        private static bool IsSubname(string name)
        {
            return !Province.ProvinceRooted(name)
                && name.Length < Hospital.MinTitleLen;
        }
    }
}
