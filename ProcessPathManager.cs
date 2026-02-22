using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using AppsTime.Models;
using AppsTime.Helpers;

namespace AppsTime.Data
{
	public static class ProcessPathManager
	{
		private static readonly string FilePath = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory, "paths.json");

		// 👇 Настройки JSON без экранирования специальных символов
		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // 👇 Разрешаем все символы Unicode
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

			// 👇 Проверяем и обновляем/добавляем запись
			if (data.ProcessPaths.ContainsKey(processName))
			{
				if (data.ProcessPaths[processName] != processPath)
				{
					// Путь изменился - обновляем
					data.ProcessPaths[processName] = processPath;
					AppLogger.Log($"[ProcessPath] Обновлён путь: {processName} → {processPath}");
				}
			}
			else
			{
				// Новый процесс - добавляем
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
	}
}