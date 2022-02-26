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
        public List<string> Names { get; private set; }
        public string[] Subnames { get; private set; }

        public static Hospital Parse(string entry)
        {
            List<string> annotations;
            var cleanedup = CleanUp(entry, out annotations);
            var names = Reconcile(cleanedup, annotations).Select(n => n.Replace("附属", null)).ToArray();
            var subnames = new List<string>(annotations.Count);
            foreach (var annotation in annotations)
            {
                Trace.Assert(!annotation.Contains(Delimiter));
                var anno = annotation;
                foreach (var subEndTag in SubEndTags)
                {
                    anno = anno.Replace(subEndTag, $"{Delimiter}");
                }
                if (!anno.Contains(Delimiter)) continue;
                subnames.AddRange(anno.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries));
            }
            for (var i = 0; i < names.Length; ++i)
            {
                var idx = names[i].IndexOf("医院");
                if (0 <= idx && idx < names[i].Length - 2)
                {
                    var subname = names[i].Substring(idx + 2, names[i].Length - idx - 2);
                    foreach (var subEndTag in SubEndTags)
                    {
                        subname = subname.Replace(subEndTag, null);
                    }
                    subnames.Add(subname);
                    names[i] = names[i].Substring(0, idx + 2);
                }
            }
            return new Hospital
            {
                OriginalEntry = entry,
                Names = names.Distinct().ToList(),
                Subnames = subnames.Distinct().ToArray()
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

        public static string CleanUp(string entry, out List<string> annotations)
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
                { '）', ')' },
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
            var j = 0;
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
                                            annotations.Add(builder.ToString(k, i - k));
                                            break;
                                        }
                                    default:
                                        {
                                            builder2.Append(builder[i]);
                                            break;
                                        }
                                }
                            }
                            annotations.Add(builder2.ToString());
                            break;
                        }
                    case AliasTagChr: // alias
                        {
                            builder[j++] = Delimiter;
                            while (++i < builder.Length && builder[i] != ')')
                            {
                                switch (builder[i])
                                {
                                    case '(':
                                    case AliasTagChr:
                                        {
                                            var k = i + 1;
                                            while (++i < builder.Length && builder[i] != ')') ;
                                            annotations.Add(builder.ToString(k, i - k));
                                            break;
                                        }
                                    default:
                                        {
                                            builder[j++] = builder[i];
                                            break;
                                        }
                                }
                            }
                            if (j < builder.Length) builder[j++] = Delimiter;
                            break;
                        }
                    case ')': break;
                    default:
                        {
                            builder[j++] = builder[i];
                            break;
                        }
                }
            }
            builder.Length = j;
            annotations = annotations.Distinct().ToList();

            return builder.ToString();
        }

        private static string[] SplitNames(string str)
        {
            // required to be two-char string
            var endTags = new[] { "医院" }.Concat(SubEndTags).ToArray();
            var names = new List<string>();
            foreach (var block in str.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries))
            {
                var i = block.Length - 1;
                while (i >= 0)
                {
                    var idx1 = block.LastIndexOf("医院", i, i + 1);
                    if (idx1 == -1)
                    {
                        names.Add(block.Substring(0, i + 1));
                        i = -1;
                    }
                    else
                    {
                        var idx2 = idx1 - 1;
                        if (idx2 >= 0)
                        {
                            idx2 = endTags
                                .Select(tag => block.LastIndexOf(tag, idx2, idx2 + 1))
                                .Max();
                        }
                        idx2 = idx2 == -1 ? 0 : (idx2 + 2);
                        names.Add(block.Substring(idx2, i + 1 - idx2));
                        i = idx2 - 1;
                    }
                }
                Trace.Assert(i == -1);
            }
            return names.Distinct().ToArray();
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
            var names = new List<string>(SplitNames(str));
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
                names.AddRange(SplitNames(annotations[i]));
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
