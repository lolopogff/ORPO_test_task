using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NReco.Text;

class Program
{
    static List<string> forbiddenOrgs = new();
    static readonly string[] inputFiles =
    {
        "Список документов ams.txt",
        "Список документов arb.txt",
        "Список документов r002.txt"
    };

    static AhoCorasickDoubleArrayTrie<string>? ahoAutomaton;

    class Violation
    {
        public string FileName { get; set; } = "";
        public int GlobalBlockNumber { get; set; }
        public List<string> Participants { get; set; } = new();
    }

    class AlgorithmResult
    {
        public string Name { get; set; } = "";
        public long ExecutionTimeMs { get; set; }
        public int TotalBlocks { get; set; }
        public int BadBlocksFound { get; set; }
        public int AlgorithmNumber { get; set; }
        public List<Violation> Violations { get; set; } = new();

        public double BlocksPerSecond => TotalBlocks > 0 ? TotalBlocks / (ExecutionTimeMs / 1000.0) : 0;
        public double TimePerBlock => TotalBlocks > 0 ? ExecutionTimeMs / (double)TotalBlocks : 0;
    }

    static List<AlgorithmResult> results = new();

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;


        string logFileName = $"Запрещённые_организации_лог_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

        var fileStream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        var fileWriter = new StreamWriter(fileStream, Encoding.UTF8)
        {
            AutoFlush = true  
        };

        var originalOut = Console.Out;
        var dualWriter = new DualTextWriter(originalOut, fileWriter);

        Console.SetOut(dualWriter);

        PrintHeader("АНАЛИЗАТОР ЗАПРЕЩЕННЫХ ОРГАНИЗАЦИЙ");
        Console.WriteLine();

        var encoding = Encoding.UTF8;

        if (!File.Exists("Запрещенные организации.txt"))
        {
            PrintError("Файл 'Запрещенные организации.txt' не найден.");
            goto Cleanup;
        }

        Console.Write("Загрузка списка запрещенных организаций... ");
        forbiddenOrgs = File.ReadAllLines("Запрещенные организации.txt", encoding)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Select(l => l.Trim().ToLowerInvariant())
                            .Distinct()
                            .ToList();

        PrintSuccess($"Загружено {forbiddenOrgs.Count} организаций");
        Console.WriteLine();

        Console.Write("Построение Aho-Corasick автомата... ");
        var dict = forbiddenOrgs.ToDictionary(o => o, o => o);
        ahoAutomaton = new AhoCorasickDoubleArrayTrie<string>();
        ahoAutomaton.Build(dict);
        PrintSuccess("Готово");
        Console.WriteLine();

        PrintStatistics();

        PrintHeader("ЗАПУСК АЛГОРИТМОВ ПРОВЕРКИ");
        Console.WriteLine();

        var result1 = RunAlgorithm("Простой двойной цикл (Contains)", CheckWithSimpleContains, 1);
        results.Add(result1);

        var result2 = RunAlgorithm("HashSet (точные совпадения)", CheckWithHashSetExactMatch, 2);
        results.Add(result2);

        var result3 = RunAlgorithm("Оптимизированный IndexOf", CheckWithOptimizedIndexOf, 3);
        results.Add(result3);

        var result4 = RunAlgorithm("Aho-Corasick автомат", CheckWithAhoCorasick, 4);
        results.Add(result4);

        PrintResultsTable();

        PrintHeader("НАЙДЕННЫЕ БЛОКИ С ЗАПРЕЩЁННЫМИ ОРГАНИЗАЦИЯМИ");

        var allViolations = results
            .SelectMany(r => r.Violations)
            .GroupBy(v => (v.FileName, v.GlobalBlockNumber))
            .Select(g => g.First())
            .OrderBy(v => v.FileName)
            .ThenBy(v => v.GlobalBlockNumber)
            .ToList();

        if (allViolations.Any())
        {
            Console.WriteLine($"Всего уникальных блоков с нарушениями: {allViolations.Count:N0}\n");

            string currentFile = "";
            foreach (var violation in allViolations)
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
        else
        {
            PrintSuccess("✓ Запрещенные организации не обнаружены ни в одном блоке.");
        }

        PrintFinalStatisticsTable(allViolations.Count, results);

        Console.WriteLine($"\n{new string('═', 60)}");
        Console.WriteLine($"Лог сохранён в файл: {logFileName}");
        Console.WriteLine("Проверка завершена. Нажмите любую клавишу для выхода...");

    Cleanup:
        Console.ReadKey();

        Console.SetOut(originalOut);
        fileWriter.Dispose();
        fileStream.Dispose();
    }

    class DualTextWriter : TextWriter
    {
        private readonly TextWriter console;
        private readonly TextWriter file;

        public DualTextWriter(TextWriter console, TextWriter file)
        {
            this.console = console;
            this.file = file;
        }

        public override Encoding Encoding => console.Encoding;

        public override void Write(char value)
        {
            console.Write(value);
            file.Write(value);
        }

        public override void Write(string? value)
        {
            console.Write(value);
            file.Write(value);
        }

        public override void WriteLine(string? value)
        {
            console.WriteLine(value);
            file.WriteLine(value);
        }

        public override void Flush()
        {
            console.Flush();
            file.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                file?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    static void PrintFinalStatisticsTable(int uniqueViolationsCount, List<AlgorithmResult> results)
    {
        PrintHeader("ИТОГОВАЯ СТАТИСТИКА");
        Console.WriteLine();

        int totalBlocks = results.FirstOrDefault()?.TotalBlocks ?? 0;
        long totalTimeAllAlgorithms = results.Sum(r => r.ExecutionTimeMs);
        double violationPercent = totalBlocks > 0 ? (double)uniqueViolationsCount / totalBlocks * 100 : 0;

        var fastest = results.OrderBy(r => r.ExecutionTimeMs).First();
        var slowest = results.OrderByDescending(r => r.ExecutionTimeMs).First();
        double speedup = slowest.ExecutionTimeMs > 0 ? (double)slowest.ExecutionTimeMs / fastest.ExecutionTimeMs : 1;

        Console.WriteLine("┌──────────────────────────────────────────────────────────────┬──────────────────────────────────┐");
        Console.WriteLine("│ Показатель                                                   │ Значение                         │");
        Console.WriteLine("├──────────────────────────────────────────────────────────────┼──────────────────────────────────┤");

        PrintTableRow("Обработано файлов", inputFiles.Length.ToString());
        PrintTableRow("Всего проверено блоков", totalBlocks.ToString("N0"));
        PrintTableRow("Уникальных блоков с нарушениями", uniqueViolationsCount.ToString("N0"));
        PrintTableRow("Процент блоков с нарушениями", $"{violationPercent:F2} %");
        PrintTableRow("Общее время всех алгоритмов", FormatTime(totalTimeAllAlgorithms));
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

    static void PrintTableRow(string label, string value)
    {
        Console.WriteLine($"│ {label.PadRight(60)} │ {value.PadLeft(32)} │");
    }

    static AlgorithmResult RunAlgorithm(string name, Func<List<string>, bool> checkFunc, int algorithmNumber)
    {
        Console.Write($"[{algorithmNumber}] Запуск {name}... ");
        var sw = Stopwatch.StartNew();

        int totalBlocks = 0;
        int badBlocksCount = 0;
        var violations = new List<Violation>();
        int globalBlockCounter = 0;

        foreach (var file in inputFiles)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"\nПредупреждение: файл '{file}' не найден.");
                continue;
            }

            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            var currentBlockOriginal = new List<string>();

            for (int i = 0; i <= lines.Length; i++)
            {
                bool isEndOfBlock = i == lines.Length || string.IsNullOrWhiteSpace(lines[i]);

                if (isEndOfBlock)
                {
                    if (currentBlockOriginal.Count > 0)
                    {
                        globalBlockCounter++;
                        totalBlocks++;

                        var normalizedBlock = currentBlockOriginal
                            .Select(line => line.Trim().ToLowerInvariant())
                            .ToList();

                        if (checkFunc(normalizedBlock))
                        {
                            badBlocksCount++;
                            violations.Add(new Violation
                            {
                                FileName = file,
                                GlobalBlockNumber = globalBlockCounter,
                                Participants = new List<string>(currentBlockOriginal)
                            });
                        }

                        currentBlockOriginal.Clear();
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        currentBlockOriginal.Add(lines[i]);
                    }
                }
            }
        }

        sw.Stop();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("✓ Готово ");
        Console.ResetColor();
        Console.Write($"(нарушений: ");
        Console.ForegroundColor = badBlocksCount > 0 ? ConsoleColor.Red : ConsoleColor.Green;
        Console.Write($"{badBlocksCount:N0}");
        Console.ResetColor();
        Console.WriteLine($", время: {FormatTime(sw.ElapsedMilliseconds)})");

        return new AlgorithmResult
        {
            Name = name,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            TotalBlocks = totalBlocks,
            BadBlocksFound = badBlocksCount,
            AlgorithmNumber = algorithmNumber,
            Violations = violations
        };
    }

    static bool CheckWithSimpleContains(List<string> normalizedBlock)
    {
        foreach (var line in normalizedBlock)
            foreach (var forbidden in forbiddenOrgs)
                if (line.Contains(forbidden))
                    return true;
        return false;
    }

    static bool CheckWithHashSetExactMatch(List<string> normalizedBlock)
    {
        var blockSet = new HashSet<string>(normalizedBlock);
        foreach (var forbidden in forbiddenOrgs)
            if (blockSet.Contains(forbidden))
                return true;
        return false;
    }

    static bool CheckWithOptimizedIndexOf(List<string> normalizedBlock)
    {
        foreach (var line in normalizedBlock)
            foreach (var forbidden in forbiddenOrgs)
                if (line.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
        return false;
    }

    static bool CheckWithAhoCorasick(List<string> normalizedBlock)
    {
        if (ahoAutomaton == null) return false;

        foreach (var line in normalizedBlock)
        {
            var matches = ahoAutomaton.ParseText(line);
            if (matches.Any())
                return true;
        }
        return false;
    }

    static void PrintStatistics()
    {
        PrintHeader("СТАТИСТИКА ДАННЫХ");
        Console.WriteLine();

        var statsBox = new List<string>
        {
            $" Всего организаций: {forbiddenOrgs.Count:N0}",
            $" Средняя длина: {forbiddenOrgs.Average(o => o.Length):F1} символов",
            $" Минимальная длина: {forbiddenOrgs.Min(o => o.Length)} символов",
            $" Максимальная длина: {forbiddenOrgs.Max(o => o.Length)} символов"
        };

        PrintBox(statsBox);
        Console.WriteLine();
    }

    static void PrintResultsTable()
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

    static string FormatTime(long milliseconds)
    {
        if (milliseconds < 1000) return $"{milliseconds} мс";
        if (milliseconds < 60000) return $"{milliseconds / 1000.0:F2} сек";
        return $"{milliseconds / 60000.0:F2} мин";
    }

    static string Truncate(string text, int maxLength)
    {
        return string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }

    static void PrintHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('═', text.Length + 4));
        Console.WriteLine($"  {text}  ");
        Console.WriteLine(new string('═', text.Length + 4));
        Console.ResetColor();
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    static void PrintBox(List<string> lines)
    {
        var maxLength = lines.Max(l => l.Length) + 4;
        Console.WriteLine("┌" + new string('─', maxLength - 2) + "┐");
        foreach (var line in lines)
            Console.WriteLine($"│ {line.PadRight(maxLength - 4)} │");
        Console.WriteLine("└" + new string('─', maxLength - 2) + "┘");
    }
}