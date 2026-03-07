using System;
using System.Diagnostics;

namespace AppsTime.Helpers
{
	public static class AppLogger
	{
		// 👇 Глобальный переключатель (оставляем для совместимости)
		public static bool DEBUG = false;

		// 👇 ПЕРЕКЛЮЧАТЕЛИ ПО КАТЕГОРИЯМ (по умолчанию true = включено)
		public static bool LogCustomData { get; set; } = false;
		public static bool LogParser { get; set; } = false;
		public static bool LogGraph { get; set; } = true;
		public static bool LogUI { get; set; } = true;
		public static bool LogTray { get; set; } = true;
		public static bool LogTimer { get; set; } = true;
		public static bool LogIcon { get; set; } = true;
		public static bool LogMenu { get; set; } = true;
		public static bool LogProcessPath { get; set; } = true;
		public static bool LogTotalTime { get; set; } = true;
		public static bool LogLang { get; set; } = true;
		public static bool LogAutoStart { get; set; } = true;

		// 👇 Ошибки и предупреждения — всегда пишутся (если DEBUG = true)
		public static bool LogErrors { get; set; } = true;
		public static bool LogWarnings { get; set; } = true;

		/// <summary>
		/// Записывает лог с категорией (с проверкой переключателя)
		/// </summary>
		[Conditional("DEBUG")]
		public static void Log(string message, string category = "")
		{
			if (!DEBUG) return;

			// Если категория не указана — пишем всегда
			if (string.IsNullOrEmpty(category))
			{
				Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
				return;
			}

			// Проверяем переключатель по категории
			bool enabled = category switch
			{
				"CustomData" => LogCustomData,
				"Parser" => LogParser,
				"Graph" or "Graph.Today" => LogGraph,
				"UI" => LogUI,
				"Tray" => LogTray,
				"Timer" => LogTimer,
				"Icon" => LogIcon,
				"Menu" => LogMenu,
				"ProcessPath" => LogProcessPath,
				"TotalTime" => LogTotalTime,
				"Lang" => LogLang,
				"AutoStart" => LogAutoStart,
				_ => true  // Неизвестная категория — пишем по умолчанию
			};

			if (enabled)
			{
				Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}");
			}
		}

		/// <summary>
		/// Записывает ошибку (всегда, если LogErrors = true)
		/// </summary>
		[Conditional("DEBUG")]
		public static void LogError(string message)
		{
			if (DEBUG && LogErrors)
				Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [❌ ERROR] {message}");
		}

		/// <summary>
		/// Записывает предупреждение (всегда, если LogWarnings = true)
		/// </summary>
		[Conditional("DEBUG")]
		public static void LogWarn(string message)
		{
			if (DEBUG && LogWarnings)
				Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [⚠️ WARN] {message}");
		}

		/// <summary>
		/// Включает все переключатели логов
		/// </summary>
		public static void EnableAllLogs()
		{
			LogCustomData = true;
			LogParser = true;
			LogGraph = true;
			LogUI = true;
			LogTray = true;
			LogTimer = true;
			LogIcon = true;
			LogMenu = true;
			LogProcessPath = true;
			LogTotalTime = true;
			LogLang = true;
			LogAutoStart = true;
		}

		/// <summary>
		/// Выключает все переключатели логов (кроме ошибок)
		/// </summary>
		public static void DisableAllLogs()
		{
			LogCustomData = false;
			LogParser = false;
			LogGraph = false;
			LogUI = false;
			LogTray = false;
			LogTimer = false;
			LogIcon = false;
			LogMenu = false;
			LogProcessPath = false;
			LogTotalTime = false;
			LogLang = false;
			LogAutoStart = false;
		}
	}
}