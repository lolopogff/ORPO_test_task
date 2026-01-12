using System;
using System.Collections.Generic;
using System.Linq;

using NReco.Text;

using ForbiddenOrgChecker;

namespace ForbiddenOrgChecker.Algorithms
{
    public static class CheckAlgorithms
    {
        public static bool CheckWithSimpleContains(List<string> normalizedBlock)
        {
            foreach (var line in normalizedBlock)
                foreach (var forbidden in AppData.ForbiddenOrgs)
                    if (line.Contains(forbidden))
                        return true;
            return false;
        }

        public static bool CheckWithHashSetExactMatch(List<string> normalizedBlock)
        {
            var blockSet = new HashSet<string>(normalizedBlock);
            foreach (var forbidden in AppData.ForbiddenOrgs)
                if (blockSet.Contains(forbidden))
                    return true;
            return false;
        }

        public static bool CheckWithOptimizedIndexOf(List<string> normalizedBlock)
        {
            foreach (var line in normalizedBlock)
                foreach (var forbidden in AppData.ForbiddenOrgs)
                    if (line.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
            return false;
        }

        public static bool CheckWithAhoCorasick(List<string> normalizedBlock)
        {
            if (AppData.AhoAutomaton == null) return false;

            foreach (var line in normalizedBlock)
            {
                var matches = AppData.AhoAutomaton.ParseText(line);
                if (matches.Any())
                    return true;
            }
            return false;
        }
    }
}