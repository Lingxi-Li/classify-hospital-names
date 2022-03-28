using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Label.Strategy
{
    class SubnameConverter
    {
        // must be two-char long
        public const string EndTag = "注解";

        public SubnameConverter(IEnumerable<KeyValuePair<string, string>> map)
        {
            kvs = new List<KeyValuePair<string, string>>(
                map.Select(kv => new KeyValuePair<string, string>(kv.Key, $"{kv.Value}{EndTag}"))
            );
        }

        public StringBuilder Convert(ref StringBuilder builder)
        {
            foreach (var kv in kvs)
            {
                builder.Replace(kv.Key, kv.Value);
            }
            var str = builder.ToString();
            var p = 0;
            while ((p = str.IndexOf("医院第", p)) != -1)
            {
                var pp = str.IndexOf("医院", p + 3);
                if (pp == -1) break;
                str = $"{str.Substring(0, pp)}{EndTag}{str.Substring(pp + 2, str.Length - pp - 2)}";
                p = pp + 2;
            }
            return builder = new StringBuilder(str);
        }

        private List<KeyValuePair<string, string>> kvs;
    }
}
