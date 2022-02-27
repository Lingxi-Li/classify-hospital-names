using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Label
{
    class Hospital
    {
        public string OriginalEntry { get; private set; }
        public string NormalizedEntry { get; private set; }
        public List<string> Names { get; private set; }
        public string[] Subnames { get; private set; }
        public string[] Annotations { get; private set; }

        public static Hospital Parse(string entry)
        {
            var moreSubEndTags = SubEndTags.Concat(new[] { "医院", "院", "部", "区" }).ToArray();

            List<string> annotations;
            string normalizedEntry;
            var cleanedup = CleanUp(entry, out annotations, out normalizedEntry);
            var names = Reconcile(cleanedup, annotations).Select(n => n.Replace("附属", null)).ToArray();
            var subnames = new List<string>(annotations.Count);
            var annos = new List<string>(annotations.Count);
            foreach (var annotation in annotations)
            {
                var subEndTag = SubEndTags.FirstOrDefault(tag => annotation.EndsWith(tag));
                if (subEndTag != null)
                {
                    var subname = annotation.Substring(0, annotation.Length - subEndTag.Length);
                    if (subname.Length == 0)
                    {
                        subnames.AddRange(SubEndTags);
                        continue;
                    }
                    subnames.AddRange(moreSubEndTags.Select(tag => $"{subname}{tag}"));
                    if (subname.Length >= 2) subnames.Add(subname);
                }
                else
                {
                    annos.Add(annotation);
                }
            }
            for (var i = 0; i < names.Length; ++i)
            {
                var idx = names[i].LastIndexOf("医院");
                if (0 <= idx && idx < names[i].Length - 2)
                {
                    var endTag = SubEndTags.FirstOrDefault(tag => names[i].EndsWith(tag));
                    var subname = names[i].Substring(idx + 2, names[i].Length - idx - 2);
                    if (endTag != null)
                    {
                        subname = subname.Substring(0, subname.Length - endTag.Length);
                        if (subname.Length == 0)
                        {
                            var idx2 = idx == 0 ? -1 : names[i].LastIndexOf("医院", idx - 1, idx);
                            if (idx2 != -1)
                            {
                                subname = names[i].Substring(idx2 + 2, idx - idx2 - "医院".Length);
                                idx = idx2;
                            }
                        }
                        subnames.AddRange(subname.Length == 0 ? SubEndTags : (moreSubEndTags.Select(tag => $"{subname}{tag}")));
                        if (subname.Length >= 2) subnames.Add(subname);
                    }
                    else
                    {
                        Trace.Assert(subname.Length > 0);
                        subnames.AddRange(moreSubEndTags.Select(tag => $"{subname}{tag}"));
                        subnames.Add(subname);
                    }
                    names[i] = names[i].Substring(0, idx + 2);
                }
            }
            return new Hospital
            {
                OriginalEntry = entry,
                NormalizedEntry = normalizedEntry,
                Names = names.NonEmptyDistinct().ToList(),
                Subnames = subnames.NonEmptyDistinct().ToArray(),
                Annotations = annos.NonEmptyDistinct().ToArray()
            };
        }

        ////////////////////

        private const char Delimiter = '〓';
        private const char AliasTagChr = '{';

        // required to be two-char string
        private static string[] SubEndTags = new[]
        {
            "分院",
            "分部",
            "院区"
        };

        public static string CleanUp(string entry, out List<string> annotations, out string normalizedEntry)
        {
            // char by char processing
            var remove = new HashSet<char>
            {
                ' ',
                '　'
            };
            var map = new Dictionary<char, char>
            {
                { '（', '(' },
                { '[', '(' },
                { '【', '(' },
                { '｛', '(' },
                { '<', '(' },
                { '《', '(' },
                { '）', ')' },
                { ']', ')' },
                { '】', ')' },
                { '｝', ')' },
                { '>', ')' },
                { '》', ')' },
                { '：', ':' },
                { '0', '零' },
                { '1', '一' },
                { '2', '二' },
                { '3', '三' },
                { '4', '四' },
                { '5', '五' },
                { '6', '六' },
                { '7', '七' },
                { '8', '八' },
                { '9', '九' },
                { '、', Delimiter },
                { '，', Delimiter },
                { '；', Delimiter },
                { '。', Delimiter },
                { ',', Delimiter },
                { ';', Delimiter },
                { '.', Delimiter },
                { '/', Delimiter },
                { '\\', Delimiter },
                { '醫', '医' },
                { '區', '区' },
                { '屬', '属' }
            };
            var builder = new StringBuilder(entry.Length);
            foreach (var c in entry)
            {
                if (remove.Contains(c)) continue;
                builder.Append(map.TryGetValue(c, out char mapped) ? mapped : c);
            }
            normalizedEntry = builder.ToString();

            // process annotations
            var AliasTagStrs = new[]
            {
                "(原:",
                "(原",
                "(:"
            };
            annotations = new List<string>();
            foreach (var tag in AliasTagStrs)
            {
                builder.Replace(tag, $"{AliasTagChr}");
            }
            var resBuilder = new StringBuilder(builder.Length);
            for (int i = 0; i < builder.Length; ++i)
            {
                switch (builder[i])
                {
                    case '(': // annotation
                        {
                            var builder2 = new StringBuilder();
                            while (++i < builder.Length && builder[i] != ')')
                            {
                                switch (builder[i])
                                {
                                    case '(':
                                        {
                                            var k = i + 1;
                                            while (++i < builder.Length && builder[i] != ')') ;
                                            annotations.AddRange(Split(builder.ToString(k, i - k)));
                                            break;
                                        }
                                    default:
                                        {
                                            builder2.Append(builder[i]);
                                            break;
                                        }
                                }
                            }
                            annotations.AddRange(Split(builder2.ToString()));
                            break;
                        }
                    case AliasTagChr: // alias
                        {
                            int oldi = i, oldlen = resBuilder.Length;
                            resBuilder.Append(Delimiter);
                            while (++i < builder.Length && builder[i] != ')')
                            {
                                switch (builder[i])
                                {
                                    case '(':
                                    case AliasTagChr:
                                        {
                                            var k = i + 1;
                                            while (++i < builder.Length && builder[i] != ')') ;
                                            annotations.AddRange(Split(builder.ToString(k, i - k)));
                                            break;
                                        }
                                    default:
                                        {
                                            resBuilder.Append(builder[i]);
                                            break;
                                        }
                                }
                            }
                            if (i < builder.Length - 1)
                            {
                                annotations.AddRange(Split(builder.ToString(oldi + 1, i - oldi - 1)));
                                resBuilder.Length = oldlen; // rewind
                            }
                            else
                            {
                                resBuilder.Append(Delimiter);
                            }
                            break;
                        }
                    case ')': break;
                    default:
                        {
                            resBuilder.Append(builder[i]);
                            break;
                        }
                }
            }
            annotations = annotations.Distinct().ToList();

            return resBuilder.ToString();
        }

        private static string[] Split(string str)
        {
            return str.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string RemoveNoises(string str)
        {
            var builder = new StringBuilder(str.Length);
            foreach (var c in str)
            {
                switch (c)
                {
                    case '(':
                    case AliasTagChr:
                    case ':':
                        {
                            break;
                        }
                    default:
                        {
                            builder.Append(c);
                            break;
                        }
                }
            }
            return builder.ToString();
        }

        public static string[] Reconcile(string str, List<string> annotations)
        {
            var names = new List<string>(Split(str));
            var end = 0;
            for (var i = 0; i < annotations.Count; ++i)
            {
                if (!annotations[i].Contains("医院"))
                {
                    var temp = annotations[end];
                    annotations[end++] = annotations[i];
                    annotations[i] = temp;
                }
            }
            for (var i = end; i < annotations.Count; ++i)
            {
                names.Add(annotations[i]);
            }
            annotations.RemoveRange(end, annotations.Count - end);
            for (var i = 0; i < annotations.Count; ++i)
            {
                annotations[i] = RemoveNoises(annotations[i]);
            }
            for (var i = 0; i < names.Count; ++i)
            {
                names[i] = RemoveNoises(names[i]);
            }
            return names.Distinct().ToArray();
        }
    }
}
