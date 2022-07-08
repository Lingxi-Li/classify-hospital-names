using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Label.Strategy;

namespace Label
{
    class Hospital
    {
        public const int MinTitleLen = 6;

        public string OriginalEntry { get; private set; }
        public string NormalizedEntry { get; private set; }
        public List<string> Names { get; private set; }
        public string[] Subnames { get; private set; }
        public string[] Annotations { get; private set; }

        public static Hospital Parse(string entry)
        {
            List<string> annotations;
            string normalizedEntry;
            var cleanedup = CleanUp(entry, out annotations, out normalizedEntry);
            var names = Reconcile(cleanedup, annotations).ToArray();
            var subnames = IdentifySubnames(ref annotations, names);
            var h = new Hospital
            {
                OriginalEntry = entry,
                NormalizedEntry = normalizedEntry,
                Names = names.NonEmptyDistinct().OrderBy(n => -n.Length).ToList(),
                Subnames = subnames.NonEmptyDistinct().ToArray(),
                Annotations = annotations.NonEmptyDistinct().ToArray()
            };
            return h;
        }

        private static List<string> IdentifySubnames(ref List<string> annotations, string[] names)
        {
            var subnames = new List<string>();
            var otherAnnotations = new List<string>();
            foreach (var anno in annotations)
            {
                Trace.Assert(!anno.Contains("医院"));
                var subEndTag = MoreSubEndTags.FirstOrDefault(tag => anno.EndsWith(tag));
                if (subEndTag != null)
                {
                    subnames.AddRange(GetSubnameAliases(anno, subEndTag));
                }
                else
                {
                    otherAnnotations.Add(anno);
                }
            }
            for (var i = 0; i < names.Length; ++i)
            {
                var parts = names[i].Split(SubnameDelimiter);
                Trace.Assert(parts.Length <= 2);
                names[i] = parts[0];
                if (parts.Length == 2)
                {
                    var subEndTag = MoreSubEndTags.FirstOrDefault(tag => parts[1].EndsWith(tag));
                    if (subEndTag != null)
                    {
                        subnames.AddRange(GetSubnameAliases(parts[1], subEndTag));
                    }
                    else
                    {
                        subnames.Add(parts[1]);
                        subnames.AddRange(MoreSubEndTags.Select(tag => $"{parts[1]}{tag}"));
                    }
                }
            }
            annotations = otherAnnotations;
            return subnames;            
        }

        private static string[] GetSubnameAliases(string subname, string endTag)
        {
            Trace.Assert(endTag != null);
            var payload = subname.Substring(0, subname.Length - endTag.Length);
            if (payload.Length == 0)
            {
                return SubEndTags;
            }
            else
            {
                var std = MoreSubEndTags.Select(tag => $"{payload}{tag}");
                return (payload.Length >= 2 ? std.Concat(new[] { payload }) : std).ToArray();
            }
        }

        ////////////////////

        public const char SubnameDelimiter = '\t';
        private const char Delimiter = '\n';
        private const char AliasTagChr = '\0';

        // must be two-char long
        public static string[] SubEndTags = new[]
        {
            "分院",
            "分部",
            "院区",
            SubnameConverter.EndTag
        };
        private static string[] MoreSubEndTags = SubEndTags.Concat(new[] { "医院", "院", "部", "区" }).ToArray();

        private static string Normalize(string entry)
        {
            // char by char processing
            var remove = new HashSet<char>
            {
                ' ',
                '　',
                '〓',
                '、',
                '，',
                '；',
                '。',
                ',',
                ';',
                '.',
                '/',
                '\\'
            };
            var map = new Dictionary<char, char>
            {
                { '（', '(' },
                { '[', '(' },
                { '【', '(' },
                { '{', '(' },
                { '｛', '(' },
                { '<', '(' },
                { '《', '(' },

                { '）', ')' },
                { ']', ')' },
                { '】', ')' },
                { '}', ')' },
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

                //{ '〓', Delimiter },
                //{ '、', Delimiter },
                //{ '，', Delimiter },
                //{ '；', Delimiter },
                //{ '。', Delimiter },
                //{ ',', Delimiter },
                //{ ';', Delimiter },
                //{ '.', Delimiter },
                //{ '/', Delimiter },
                //{ '\\', Delimiter },

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

            // map substring
            var substrMap = new[,]
            {
                { "附属", null },
                { "中医医院", "中医院" },
                { "中西医结合医院", "X医院" },
                { "医院大学", "H大学" },
                { "保健院", "保健医院" }
            };
            for (var i = 0; i < substrMap.GetLength(0); ++i)
            {
                builder.Replace(substrMap[i, 0], substrMap[i, 1]);
            }

            Province.Normalize(builder);
            var subnameConverter = new SubnameConverter(new[]
            {
                new KeyValuePair<string, string>("医院妇女儿童医院", "医院妇女儿童"),
                new KeyValuePair<string, string>("医院集团总医院", "医院总"),
                new KeyValuePair<string, string>("医院传染病医院", "医院传染病"),
                new KeyValuePair<string, string>("医院集团中心医院", "医院中心"),
                new KeyValuePair<string, string>("医院妇产儿童医院", "医院妇产儿童")
            });
            subnameConverter.Convert(ref builder);
            return builder.ToString();
        }

        public static string CleanUp(string entry, out List<string> annotations, out string normalizedEntry)
        {
            normalizedEntry = Normalize(entry);
            var builder = new StringBuilder(normalizedEntry);

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
            return str.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(part => HeuristicSplitter.Split(part))
                .ToArray();
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
