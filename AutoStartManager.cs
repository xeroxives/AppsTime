using System;
using System.Diagnostics;
using AppsTime.Helpers;

namespace AppsTime.Helpers
{
	public static class AutoStartManager
	{
		private const string TaskName = "AppsTime_AutoStart";

		/// <summary>
		/// Проверяет, включена ли автозагрузка
		/// </summary>
		public static bool IsEnabled()
		{
			try
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "schtasks",
						Arguments = $"/query /tn \"{TaskName}\"",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					}
				};
				process.Start();
				process.WaitForExit(2000);

				return process.ExitCode == 0;
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[AutoStart] Ошибка проверки: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Включает автозагрузку через Task Scheduler
		/// </summary>
		public static bool Enable()
		{
			try
			{
				string exePath = Process.GetCurrentProcess().MainModule?.FileName;
				if (string.IsNullOrEmpty(exePath))
					return false;

				// Создаём задачу на запуск при входе пользователя с наивысшими правами
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "schtasks",
						Arguments = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					}
				};
				process.Start();
				process.WaitForExit(5000);

				if (process.ExitCode == 0)
				{
					AppLogger.Log("[AutoStart] Автозагрузка включена (Task Scheduler)");
					return true;
				}
				else
				{
					string error = process.StandardError.ReadToEnd();
					AppLogger.LogError($"[AutoStart] Ошибка создания задачи: {error}");
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[AutoStart] Ошибка включения: {ex.Message}");
			}

			return false;
		}

		/// <summary>
		/// Отключает автозагрузку
		/// </summary>
		public static bool Disable()
		{
			try
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "schtasks",
						Arguments = $"/delete /tn \"{TaskName}\" /f",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					}
				};
				process.Start();
				process.WaitForExit(2000);

				if (process.ExitCode == 0)
				{
					AppLogger.Log("[AutoStart] Автозагрузка отключена");
					return true;
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[AutoStart] Ошибка отключения: {ex.Message}");
			}

			return false;
		}

		/// <summary>
		/// Переключает состояние автозагрузки
		/// </summary>
		public static bool Toggle()
		{
			if (IsEnabled())
				return Disable();
			else
				return Enable();
		}
	}
}