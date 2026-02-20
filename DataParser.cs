using AppsTime.Helpers;
using AppsTime.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppsTime.Parser
{
    public static class DataParser
    {
        private static readonly string BasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "digital-wellbeing", "dailylogs");

        /// <summary>
        /// Возвращает агрегированные данные: процесс → общее время (за все дни)
        /// </summary>

public static Dictionary<string, int> GetAllTimeStats()
    {
        var stats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        AppLogger.Log($"[Parser] Start. BasePath: {BasePath}");

        if (!Directory.Exists(BasePath))
        {
            AppLogger.LogError($"Directory NOT found: {BasePath}");
            return stats;
        }

        var allFiles = Directory.GetFiles(BasePath);
        AppLogger.Log($"[Parser] ВСЕГО файлов в папке: {allFiles.Length}");

        //foreach (var f in allFiles)
        //    AppLogger.Log($"[Parser] FILE: '{Path.GetFileName(f)}'");

        var logFiles = Directory.GetFiles(BasePath, "*.log");
        AppLogger.Log($"[Parser] Файлов *.log: {logFiles.Length}");

        foreach (var file in logFiles)
        {
            var fileName = Path.GetFileName(file);
            var fileDate = ExtractDateFromFileName(fileName);

            if (!fileDate.HasValue)
            {
                AppLogger.LogWarn($"Не удалось извлечь дату из '{fileName}'");
                continue;
            }

            var entries = ParseLogFile(file, fileDate.Value);
            //AppLogger.Log($"[Parser] ✅ Файл: {fileName}, записей: {entries.Count}");

            foreach (var entry in entries)
            {
                if (stats.ContainsKey(entry.ProcessName))
                    stats[entry.ProcessName] += entry.TimeSeconds;
                else
                    stats[entry.ProcessName] = entry.TimeSeconds;
            }
        }

        AppLogger.Log($"[Parser] Итого процессов: {stats.Count}");
        return stats;
    }

    /// <summary>
    /// Парсит конкретный файл по дате
    /// </summary>
    public static List<AppUsageEntry> ParseLogForDate(DateTime date)
        {
            var fileName = $"{date:MM-dd-yyyy}.log";
            var filePath = Path.Combine(BasePath, fileName);

            return File.Exists(filePath) ? ParseLogFile(filePath, date) : new List<AppUsageEntry>();
        }

        /// <summary>
        /// Возвращает список доступных дат с логами
        /// </summary>
        public static List<DateTime> GetAvailableDates()
        {
            if (!Directory.Exists(BasePath))
                return new List<DateTime>();

            return Directory.GetFiles(BasePath, "*.log")
                           .Select(Path.GetFileName)
                           .Where(IsValidFileName)
                           .Select(ExtractDateFromFileName)
                           .Where(d => d.HasValue)
                           .Select(d => d.Value)
                           .OrderByDescending(d => d)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────
        // Вспомогательные методы
        // ─────────────────────────────────────────────────────

        private static List<AppUsageEntry> ParseLogFile(string filePath, DateTime logDate)
        {
            var entries = new List<AppUsageEntry>();

            try
            {
                var lines = File.ReadAllLines(filePath)
                                .Where(line => !string.IsNullOrWhiteSpace(line));

                foreach (var line in lines)
                {
                    var parts = line.Split('\t').Select(p => p.Trim()).ToArray();
                    if (parts.Length < 2) continue;

                    var processName = parts[0];
                    if (!int.TryParse(parts[1], out int timeSeconds)) continue;

                    var description = parts.Length > 2 ? parts[2] : string.Empty;

                    entries.Add(new AppUsageEntry
                    {
                        ProcessName = processName,
                        TimeSeconds = timeSeconds,
                        Description = description,
                        LogDate = logDate
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Ошибка парсинга {filePath}: {ex.Message}");
            }

            return entries;
        }

        private static bool IsValidFileName(string fileName)
        {
            // Ожидаем формат: MM-dd-yyyy.log
            return System.Text.RegularExpressions.Regex.IsMatch(
                fileName, @"^\d{2}-\d{2}-\d{4}\.log$");
        }

        private static DateTime? ExtractDateFromFileName(string fileName)
        {
            try
            {
                var datePart = Path.GetFileNameWithoutExtension(fileName);
                return DateTime.ParseExact(datePart, "MM-dd-yyyy", null);
            }
            catch
            {
                return null;
            }
        }
    }
}