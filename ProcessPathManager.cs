using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using AppsTime.Helpers;
using AppsTime.Models;

namespace AppsTime.Data
{
    public static class ProcessPathManager
    {
        private static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "paths.json");

        // 👇 Настройки JSON с отключённым экранированием спецсимволов
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // 👇 Используем UnsafeRelaxedJsonEscaping для читаемых путей
            // (+ не будет превращаться в \u002B)
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Загружает данные о путях процессов из файла
        /// </summary>
        public static ProcessPathData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var data = JsonSerializer.Deserialize<ProcessPathData>(json, JsonOptions);
                    return data ?? new ProcessPathData();
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[ProcessPath] Ошибка загрузки: {ex.Message}");
            }

            return new ProcessPathData();
        }

        /// <summary>
        /// Сохраняет данные о путях процессов в файл
        /// </summary>
        public static bool Save(ProcessPathData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(FilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[ProcessPath] Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Обновляет или добавляет путь для процесса
        /// </summary>
        public static void UpdateProcessPath(string processName, string processPath)
        {
            if (string.IsNullOrEmpty(processName) || string.IsNullOrEmpty(processPath))
                return;

            var data = Load();

            if (data.ProcessPaths.ContainsKey(processName))
            {
                if (data.ProcessPaths[processName] != processPath)
                {
                    data.ProcessPaths[processName] = processPath;
                    AppLogger.Log($"[ProcessPath] Обновлён путь: {processName} → {processPath}");
                }
            }
            else
            {
                data.ProcessPaths[processName] = processPath;
                AppLogger.Log($"[ProcessPath] Добавлен путь: {processName} → {processPath}");
            }

            Save(data);
        }

        /// <summary>
        /// Получает путь для процесса (из файла или null)
        /// </summary>
        public static string GetProcessPath(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return null;

            var data = Load();

            if (data.ProcessPaths.TryGetValue(processName, out var path))
            {
                return path;
            }

            return null;
        }

        /// <summary>
        /// Сохраняет путь, выбранный пользователем вручную
        /// </summary>
        public static bool SaveUserSelectedPath(string processName, string processPath)
        {
            if (string.IsNullOrEmpty(processName) || string.IsNullOrEmpty(processPath))
                return false;

            try
            {
                var data = Load();

                if (data.ProcessPaths.ContainsKey(processName))
                {
                    data.ProcessPaths[processName] = processPath;
                    AppLogger.Log($"[ProcessPath] Пользователь обновил путь: {processName} → {processPath}");
                }
                else
                {
                    data.ProcessPaths.Add(processName, processPath);
                    AppLogger.Log($"[ProcessPath] Пользователь добавил путь: {processName} → {processPath}");
                }

                return Save(data);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[ProcessPath] Ошибка сохранения пути пользователя: {ex.Message}");
                return false;
            }
        }
    }
}