using System;
using System.Collections.Generic;
using System.Linq;

using ForbiddenOrgChecker.Models;

namespace ForbiddenOrgChecker.Helpers
{
    public static class ConsoleHelpers
    {
        public static void PrintHeader(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', text.Length + 4));
            Console.WriteLine($"  {text}  ");
            Console.WriteLine(new string('═', text.Length + 4));
            Console.ResetColor();
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }

        public static void PrintBox(List<string> lines)
        {
            var maxLength = lines.Max(l => l.Length) + 4;
            Console.WriteLine("┌" + new string('─', maxLength - 2) + "┐");
            foreach (var line in lines)
                Console.WriteLine($"│ {line.PadRight(maxLength - 4)} │");
            Console.WriteLine("└" + new string('─', maxLength - 2) + "┘");
        }

        public static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        public static string FormatTime(long milliseconds)
        {
            if (milliseconds < 1000) return $"{milliseconds} мс";
            if (milliseconds < 60000) return $"{milliseconds / 1000.0:F2} сек";
            return $"{milliseconds / 60000.0:F2} мин";
        }

        public static void PrintStatistics()
        {
            PrintHeader("СТАТИСТИКА ДАННЫХ");
            Console.WriteLine();

            var statsBox = new List<string>
            {
                $" Всего организаций: {AppData.ForbiddenOrgs.Count:N0}",
                $" Средняя длина: {AppData.ForbiddenOrgs.Average(o => o.Length):F1} символов",
                $" Минимальная длина: {AppData.ForbiddenOrgs.Min(o => o.Length)} символов",
                $" Максимальная длина: {AppData.ForbiddenOrgs.Max(o => o.Length)} символов"
            };

            PrintBox(statsBox);
            Console.WriteLine();
        }

        public static void PrintAlgorithmCompletion(int badBlocksCount, long elapsedMs)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✓ Готово ");
            Console.ResetColor();
            Console.Write($"(нарушений: ");
            Console.ForegroundColor = badBlocksCount > 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.Write($"{badBlocksCount:N0}");
            Console.ResetColor();
            Console.WriteLine($", время: {FormatTime(elapsedMs)})");
        }

        public static void PrintResultsTable(List<AlgorithmResult> results)
        {
            PrintHeader("РЕЗУЛЬТАТЫ АЛГОРИТМОВ");
            Console.WriteLine();

            Console.WriteLine("┌─────┬──────────────────────────────┬──────────────┬──────────────┬──────────────┬──────────────┐");
            Console.WriteLine("│ №   │ Алгоритм                     │ Время        │ Нарушений    │ Блоков/сек   │ мс/блок      │");
            Console.WriteLine("├─────┼──────────────────────────────┼──────────────┼──────────────┼──────────────┼──────────────┤");

            foreach (var result in results.OrderBy(r => r.AlgorithmNumber))
            {
                var timeColor = result.ExecutionTimeMs == results.Min(r => r.ExecutionTimeMs) ? ConsoleColor.Green :
                                result.ExecutionTimeMs == results.Max(r => r.ExecutionTimeMs) ? ConsoleColor.Red : ConsoleColor.White;

                var violationsColor = result.BadBlocksFound > 0 ? ConsoleColor.Red : ConsoleColor.Green;

                Console.Write($"│ {result.AlgorithmNumber,-3} │ {Truncate(result.Name, 28),-28} │ ");
                Console.ForegroundColor = timeColor;
                Console.Write($"{FormatTime(result.ExecutionTimeMs),-12} ");
                Console.ResetColor();

                Console.Write("│ ");
                Console.ForegroundColor = violationsColor;
                Console.Write($"{result.BadBlocksFound,12:N0} ");
                Console.ResetColor();

                Console.Write($"│ {result.BlocksPerSecond,12:F0} │ {result.TimePerBlock,12:F3} │");
                Console.WriteLine();
            }

            Console.WriteLine("└─────┴──────────────────────────────┴──────────────┴──────────────┴──────────────┴──────────────┘");
            Console.WriteLine();
        }

        public static void PrintViolations(List<Violation> violations)
        {
            string currentFile = "";
            foreach (var violation in violations)
            {
                if (violation.FileName != currentFile)
                {
                    currentFile = violation.FileName;
                    Console.WriteLine($"Файл: {currentFile}");
                }

                Console.WriteLine($"Блок №{violation.GlobalBlockNumber}:");
                foreach (var participant in violation.Participants)
                {
                    Console.WriteLine($"  • {participant}");
                }
                Console.WriteLine();
            }
        }

        public static void PrintFinalStatisticsTable(int uniqueViolationsCount, List<AlgorithmResult> results)
        {
            PrintHeader("ИТОГОВАЯ СТАТИСТИКА");
            Console.WriteLine();

            int totalBlocks = results.FirstOrDefault()?.TotalBlocks ?? 0;
            long totalTimeAll = results.Sum(r => r.ExecutionTimeMs);
            double violationPercent = totalBlocks > 0 ? (double)uniqueViolationsCount / totalBlocks * 100 : 0;

            var fastest = results.OrderBy(r => r.ExecutionTimeMs).First();
            var slowest = results.OrderByDescending(r => r.ExecutionTimeMs).First();
            double speedup = slowest.ExecutionTimeMs > 0 ? (double)slowest.ExecutionTimeMs / fastest.ExecutionTimeMs : 1;

            Console.WriteLine("┌──────────────────────────────────────────────────────────────┬──────────────────────────────────┐");
            Console.WriteLine("│ Показатель                                                   │ Значение                         │");
            Console.WriteLine("├──────────────────────────────────────────────────────────────┼──────────────────────────────────┤");

            PrintTableRow("Обработано файлов", AppData.InputFiles.Length.ToString());
            PrintTableRow("Всего проверено блоков", totalBlocks.ToString("N0"));
            PrintTableRow("Уникальных блоков с нарушениями", uniqueViolationsCount.ToString("N0"));
            PrintTableRow("Процент блоков с нарушениями", $"{violationPercent:F2} %");
            PrintTableRow("Общее время всех алгоритмов", FormatTime(totalTimeAll));
            PrintTableRow("Самый быстрый алгоритм", Truncate(fastest.Name, 40));
            PrintTableRow("Время самого быстрого", FormatTime(fastest.ExecutionTimeMs));
            PrintTableRow("Самый медленный алгоритм", Truncate(slowest.Name, 40));
            PrintTableRow("Время самого медленного", FormatTime(slowest.ExecutionTimeMs));
            PrintTableRow("Ускорение (медленный / быстрый)", $"{speedup:F1}x");

            Console.WriteLine("├──────────────────────────────────────────────────────────────┼──────────────────────────────────┤");

            foreach (var r in results.OrderBy(r => r.AlgorithmNumber))
            {
                PrintTableRow($"{r.AlgorithmNumber}. {Truncate(r.Name, 45)} — время", FormatTime(r.ExecutionTimeMs));
                PrintTableRow($"   — найденных нарушений", r.BadBlocksFound.ToString("N0"));
            }

            Console.WriteLine("└──────────────────────────────────────────────────────────────┴──────────────────────────────────┘");
            Console.WriteLine();
        }

        private static void PrintTableRow(string label, string value)
        {
            Console.WriteLine($"│ {label.PadRight(60)} │ {value.PadLeft(32)} │");
        }
    }
}