using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NReco.Text;

namespace ForbiddenOrgChecker
{
    internal static class AppData
    {
        public static readonly string[] InputFiles =
        {
            "Список документов ams.txt",
            "Список документов arb.txt",
            "Список документов r002.txt"
        };

        public static List<string> ForbiddenOrgs { get; private set; } = new();

        public static AhoCorasickDoubleArrayTrie<string>? AhoAutomaton { get; private set; }

        public static void LoadForbiddenOrganizations()
        {
            var encoding = Encoding.UTF8;
            ForbiddenOrgs = File.ReadAllLines("Запрещенные организации.txt", encoding)
                               .Where(l => !string.IsNullOrWhiteSpace(l))
                               .Select(l => l.Trim().ToLowerInvariant())
                               .Distinct()
                               .ToList();
        }

        public static void BuildAhoCorasick()
        {
            var dict = ForbiddenOrgs.ToDictionary(o => o, o => o);
            AhoAutomaton = new AhoCorasickDoubleArrayTrie<string>();
            AhoAutomaton.Build(dict);
        }
    }
}