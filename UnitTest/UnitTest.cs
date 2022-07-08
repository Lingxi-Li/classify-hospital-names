using Label;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void Tryout()
        {
            var labels = new[]
            {
                "上海市浦东医院复旦大学附属华山医院南汇分院(原:上海市浦东新区中心医院)"
            };
            var name = "十堰市太和医院/湖北医药学院(原:郧阳医学院)附属太和医院";
            var engine = new LabelEngine(labels);
            var match = engine.TryLabel(name);
            Console.WriteLine(match.OriginalEntry);
        }
    }
}
