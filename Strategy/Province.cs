using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Label.Strategy
{
    static class Province
    {
        public static StringBuilder Normalize(StringBuilder builder)
        {
            foreach (var kv in map)
            {
                builder.Replace(kv.Key, kv.Value);
            }
            return builder;
        }

        private static Dictionary<string, string> map = new Dictionary<string, string>
        {
            { "河北省", "河北" },
            { "山西省", "山西" },
            { "辽宁省", "辽宁" },
            { "吉林省", "吉林" },
            { "江苏省", "江苏" },
            { "浙江省", "浙江" },
            { "安徽省", "安徽" },
            { "福建省", "福建" },
            { "江西省", "江西" },
            { "山东省", "山东" },
            { "河南省", "河南" },
            { "湖北省", "湖北" },
            { "湖南省", "湖南" },
            { "广东省", "广东" },
            { "海南省", "海南" },
            { "四川省", "四川" },
            { "贵州省", "贵州" },
            { "云南省", "云南" },
            { "陕西省", "陕西" },
            { "甘肃省", "甘肃" },
            { "青海省", "青海" },
            { "台湾省", "台湾" },
            { "黑龙江省", "黑龙江" },

            { "内蒙古自治区", "内蒙古" },
            { "西藏自治区", "西藏" },
            { "广西壮族自治区", "广西" },
            { "宁夏回族自治区", "宁夏" },
            { "新疆维吾尔自治区", "新疆" },

            { "北京市", "北京" },
            { "天津市", "天津" },
            { "上海市", "上海" },
            { "重庆市", "重庆" },

            { "香港特别行政区", "香港" },
            { "澳门特别行政区", "澳门" }
        };
    }
}
