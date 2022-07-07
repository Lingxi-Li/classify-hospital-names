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
            var subnames = new List<string>(annotations.Count);
            var annos = new List<string>(annotations.Count);
            foreach (var annotation in annotations)
            {
                var subEndTag = SubEndTags.FirstOrDefault(tag => annotation.EndsWith(tag));
                if (subEndTag != null)
                {
                    subnames.AddRange(GetSubnameAliases(annotation, subEndTag));
                }
                else
                {
                    annos.Add(annotation);
                }
            }
            for (var i = 0; i < names.Length; ++i)
            {
                var parts = names[i].Split(new[] { SubnameDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                names[i] = parts[0];
                if (parts.Length == 2)
                {
                    var subEndTag = SubEndTags.FirstOrDefault(tag => parts[1].EndsWith(tag));
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
            return new Hospital
            {
                OriginalEntry = entry,
                NormalizedEntry = normalizedEntry,
                Names = names.NonEmptyDistinct().OrderBy(n => -n.Length).ToList(),
                Subnames = subnames.NonEmptyDistinct().ToArray(),
                Annotations = annos.NonEmptyDistinct().ToArray()
            };
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

        private const char SubnameDelimiter = '\t';
        private const char Delimiter = '\n';
        private const char AliasTagChr = '\0';

        // must be two-char long
        private static string[] SubEndTags = new[]
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
                '　'
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

                { '〓', Delimiter },
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
            var trim = new[]
            {
                "附属"
            };
            foreach (var noise in trim)
            {
                builder.Replace(noise, null);
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

        private static List<string> HeuristicSplit(string str)
        {
            Trace.Assert(str.Length > 0);
            var pos = new List<int>();
            for (var i = 0; i < str.Length; i += 2)
            {
                i = str.IndexOf("医院", i);
                if (i != -1)
                {
                    pos.Add(i);
                }
                else
                {
                    break;
                }
            }
            var names = new List<string>();
            for (int i = pos.Count - 1, end = str.Length; i >= 0; --i)
            {
                // Is this a main or sub title end tag? Assume main title end.
                var startIndex = i == 0 ? 0 : (pos[i - 1] + 2);
                var mainTitle = str.Substring(startIndex, pos[i] + 2 - startIndex);
                var subTitle = str.Substring(pos[i] + 2, end - pos[i] - 2); // may be empty
                var subEndTag = SubEndTags.FirstOrDefault(tag => subTitle.EndsWith(tag));
                // rules to reject the assumption
                if (startIndex > 0)
                {
                    if (
                        (subTitle == subEndTag) || // explicitly marked main title as sub title, e.g., "xxx医院分院"
                        (mainTitle.Length < MinTitleLen) // main title is too short; treat as sub
                    )
                    {
                        subTitle = str.Substring(startIndex, end - startIndex);
                        --i;
                        var newStartIndex = i == 0 ? 0 : (pos[i - 1] + 2);
                        mainTitle = str.Substring(newStartIndex, startIndex - newStartIndex);
                        startIndex = newStartIndex;
                    }
                }
                Trace.Assert(mainTitle.EndsWith("医院"));
                names.Add(subTitle.Length > 0 ? $"{mainTitle}{SubnameDelimiter}{subTitle}" : mainTitle);
                end = startIndex;
            }
            if (names.Count == 0) names.Add(str);
            return names;
        }

        private static string[] Split(string str)
        {
            return str.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(part => HeuristicSplit(part))
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
