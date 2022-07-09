using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Label.Strategy
{
    static class NameMatcher
    {
        private class Reject: Exception { }

        public static string[] ComputeNameSigs(string name)
        {
            return new[]
            {
                ComputeArmyUnivAffiliatedNameSig(name),
                ComputeArmyAffiliatedNameSig(name)
            };
        }

        public static bool Matches(string name, string label, string[] labelSigs)
        {
            if (GeneralMatch(name, label)) return true;
            try
            {
                return ArmyUnivAffiliatedMatch(name, labelSigs[0])
                    || ArmyAffiliatedMatch(name, labelSigs[1])
                    ;
            }
            catch (Reject)
            {
                return false;
            }
        }

        #region ArmyUnivAffiliated

        private static Regex ArmyUnivAffiliatedRegex = new Regex($"{Number}军医大学{Number}医院", RegexOptions.Compiled);

        private static string ComputeArmyUnivAffiliatedNameSig(string name)
        {
            name = name.Replace("第", null);
            return ArmyUnivAffiliatedRegex.Match(name).Value;
        }

        private static bool ArmyUnivAffiliatedMatch(string name, string labelSig)
        {
            var nameSig = ComputeArmyUnivAffiliatedNameSig(name);
            return SigsMatch(nameSig, labelSig, false);
        }

        #endregion ArmyUnivAffiliated

        #region ArmyAffiliated

        private static Regex ArmyAffiliatedTypeRegex = new Regex("(解放军|海军|陆军|空军|武警)", RegexOptions.Compiled);
        private static Regex ArmyAffiliatedNumberRegex = new Regex($"{Number}医院", RegexOptions.Compiled);

        private static string ComputeArmyAffiliatedNameSig(string name)
        {
            var type = ArmyAffiliatedTypeRegex.Match(name).Value;
            var number = ArmyAffiliatedNumberRegex.Match(name).Value;
            return type.Length > 0 && number.Length > 0 ? $"{type}{number}" : "";
        }

        private static bool ArmyAffiliatedMatch(string name, string labelSig)
        {
            var nameSig = ComputeArmyAffiliatedNameSig(name);
            return SigsMatch(nameSig, labelSig, true);
        }

        #endregion ArmyAffiliated

        private const string Number = "(零|一|二|三|四|五|六|七|八|九|十)+";

        private static bool SigsMatch(string sigA, string sigB, bool fallthrough)
        {
            if (sigA.Length == 0 && sigB.Length == 0) return false;
            return
                sigA == sigB ? true :
                fallthrough ? false :          
                throw new Reject();
        }

        private static bool GeneralMatch(string a, string b)
        {
            string sub, main;
            if (a.Length <= b.Length)
            {
                sub = a;
                main = b;
            }
            else
            {
                sub = b;
                main = a;
            }
            return
                main.EndsWith(sub)
                && (main.Length - sub.Length != 1); // e.g., 沙县中医院 and 金沙县中医院
        }
    }
}
