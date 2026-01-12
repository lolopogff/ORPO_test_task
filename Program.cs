using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

using NReco.Text;

using ForbiddenOrgChecker.Models;
using ForbiddenOrgChecker.Helpers;
using ForbiddenOrgChecker.Algorithms;

namespace ForbiddenOrgChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string logFileName = $"Запрещённые_организации_лог_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

            using var fileStream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var fileWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };

            var originalOut = Console.Out;
            var dualWriter = new DualTextWriter(originalOut, fileWriter);
            Console.SetOut(dualWriter);

            try
            {
                RunApplication();
                Console.WriteLine($"\nЛог сохранён в файл: {logFileName}");
            }
            catch (Exception ex)
            {
                ConsoleHelpers.PrintError($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Проверка завершена. Нажмите любую клавишу для выхода...");
                Console.ReadKey();

                Console.SetOut(originalOut);
            }
        }

        private static void RunApplication()
        {
            ConsoleHelpers.PrintHeader("АНАЛИЗАТОР ЗАПРЕЩЕННЫХ ОРГАНИЗАЦИЙ");
            Console.WriteLine();

            if (!File.Exists("Запрещенные организации.txt"))
            {
                ConsoleHelpers.PrintError("Файл 'Запрещенные организации.txt' не найден.");
                return;
            }

            Console.Write("Загрузка списка запрещенных организаций... ");
            AppData.LoadForbiddenOrganizations();
            ConsoleHelpers.PrintSuccess($"Загружено {AppData.ForbiddenOrgs.Count} организаций");
            Console.WriteLine();

            Console.Write("Построение Aho-Corasick автомата... ");
            AppData.BuildAhoCorasick();
            ConsoleHelpers.PrintSuccess("Готово");
            Console.WriteLine();

            ConsoleHelpers.PrintStatistics();

            ConsoleHelpers.PrintHeader("ЗАПУСК АЛГОРИТМОВ ПРОВЕРКИ");
            Console.WriteLine();

            var results = new List<AlgorithmResult>
            {
                RunAlgorithm("Простой двойной цикл (Contains)", CheckAlgorithms.CheckWithSimpleContains, 1),
                RunAlgorithm("HashSet (точные совпадения)", CheckAlgorithms.CheckWithHashSetExactMatch, 2),
                RunAlgorithm("Оптимизированный IndexOf", CheckAlgorithms.CheckWithOptimizedIndexOf, 3),
                RunAlgorithm("Aho-Corasick автомат", CheckAlgorithms.CheckWithAhoCorasick, 4)
            };

            ConsoleHelpers.PrintResultsTable(results);

            var allViolations = results
                .SelectMany(r => r.Violations)
                .GroupBy(v => (v.FileName, v.GlobalBlockNumber))
                .Select(g => g.First())
                .OrderBy(v => v.FileName)
                .ThenBy(v => v.GlobalBlockNumber)
                .ToList();

            ConsoleHelpers.PrintHeader("НАЙДЕННЫЕ БЛОКИ С ЗАПРЕЩЁННЫМИ ОРГАНИЗАЦИЯМИ");

            if (allViolations.Any())
            {
                Console.WriteLine($"Всего уникальных блоков с нарушениями: {allViolations.Count:N0}\n");
                ConsoleHelpers.PrintViolations(allViolations);
            }
            else
            {
                ConsoleHelpers.PrintSuccess("✓ Запрещенные организации не обнаружены ни в одном блоке.");
            }

            ConsoleHelpers.PrintFinalStatisticsTable(allViolations.Count, results);
            Console.WriteLine($"\n{new string('═', 60)}");
        }

        private static AlgorithmResult RunAlgorithm(string name, Func<List<string>, bool> checkFunc, int algorithmNumber)
        {
            Console.Write($"[{algorithmNumber}] Запуск {name}... ");
            var sw = Stopwatch.StartNew();

            int totalBlocks = 0;
            int badBlocksCount = 0;
            var violations = new List<Violation>();
            int globalBlockCounter = 0;

            foreach (var file in AppData.InputFiles)
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
                    else if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        currentBlockOriginal.Add(lines[i]);
                    }
                }
            }

            sw.Stop();

            ConsoleHelpers.PrintAlgorithmCompletion(badBlocksCount, sw.ElapsedMilliseconds);

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
    }
}