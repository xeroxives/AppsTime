using AppsTime.Helpers;
using AppsTime.Models;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppsTime.Data
{
    public static class CustomDataManager
    {
        private static readonly string FileName = "custom-logs.json";
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

        // 👇 Настройки JSON для красивого форматирования
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Загружает пользовательские данные из файла
        /// </summary>
        public static CustomData Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    AppLogger.Log("[CustomData] Файл не найден, создаём новый");
                    return new CustomData();
                }

                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<CustomData>(json, JsonOptions);

                AppLogger.Log($"[CustomData] Загружено: {data?.NameAliases.Count ?? 0} алиасов, " +
                             $"{data?.TimeOverrides.Count ?? 0} изменений времени, " +
                             $"{data?.ExcludedProcesses.Count ?? 0} исключений");

                return data ?? new CustomData();
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[CustomData] Ошибка загрузки: {ex.Message}");
                return new CustomData();
            }
        }

        /// <summary>
        /// Сохраняет пользовательские данные в файл
        /// </summary>
        public static bool Save(CustomData data)
        {
            try
            {
                data.LastModified = DateTime.Now;
                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(FilePath, json);

                AppLogger.Log($"[CustomData] Сохранено в {FilePath}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[CustomData] Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, существует ли файл сохранений
        /// </summary>
        public static bool Exists() => File.Exists(FilePath);

        /// <summary>
        /// Возвращает путь к файлу сохранений
        /// </summary>
        public static string GetFilePath() => FilePath;
    }
}