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
                "阜南县中医院"
            };
            var name = "阜南中医院";
            var engine = new LabelEngine(labels);
            var match = engine.TryLabel(name);
            Console.WriteLine(match.OriginalEntry);
        }
    }
}
