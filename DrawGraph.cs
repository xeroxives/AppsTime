using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Wpf;

namespace AppsTime.Helpers
{
	public class DrawGraph
	{
		private readonly string _logDirectory;
		private readonly Dictionary<string, Dictionary<DateTime, int>> _cache
			= new Dictionary<string, Dictionary<DateTime, int>>();

		public enum DateRange
		{
			Week,
			Month,
			Year,
			All
		}

		public DrawGraph()
		{
			_logDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"digital-wellbeing",
				"dailylogs"
			);
		}

		// Загружает все данные в кэш при старте приложения
		public async Task InitializeCacheAsync()
		{
			await Task.Run(() =>
			{
				if (!Directory.Exists(_logDirectory))
				{
					AppLogger.Log($"[Graph] Директория не найдена: {_logDirectory}");
					return;
				}

				var logFiles = Directory.GetFiles(_logDirectory, "*.log");
				AppLogger.Log($"[Graph] Найдено логов: {logFiles.Length}");

				int totalProcesses = 0;

				foreach (var file in logFiles)
				{
					try
					{
						var fileName = Path.GetFileNameWithoutExtension(file);

						if (!DateTime.TryParseExact(fileName, "MM-dd-yyyy", null,
							System.Globalization.DateTimeStyles.None, out var date))
						{
							continue;
						}

						var processes = ParseAllProcesses(file);
						totalProcesses += processes.Count;

						foreach (var kvp in processes)
						{
							AppLogger.Log($"[Graph] {date:MM-dd} | {kvp.Key} | {kvp.Value}s");
						}

						foreach (var kvp in processes)
						{
							if (!_cache.ContainsKey(kvp.Key))
							{
								_cache[kvp.Key] = new Dictionary<DateTime, int>();
							}
							_cache[kvp.Key][date] = kvp.Value;
						}
					}
					catch (Exception ex)
					{
						AppLogger.LogError($"[Graph] Ошибка чтения {file}: {ex.Message}");
					}
				}

				AppLogger.Log($"[Graph] Всего процессов в кэше: {_cache.Count}");
				AppLogger.Log($"[Graph] Всего записей: {totalProcesses}");
			});
		}

		// Парсит все процессы из одного файла лога
		private Dictionary<string, int> ParseAllProcesses(string filePath)
		{
			var result = new Dictionary<string, int>();

			try
			{
				foreach (var line in File.ReadLines(filePath))
				{
					if (string.IsNullOrWhiteSpace(line))
						continue;

					var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

					if (parts.Length < 2)
						continue;

					var processName = parts[0].Trim().ToLower();

					if (int.TryParse(parts[1], out int seconds) && seconds > 0)
					{
						if (!result.ContainsKey(processName))
						{
							result[processName] = 0;
						}
						result[processName] += seconds;
					}
				}
			}
			catch
			{
				// Игнорируем ошибки парсинга
			}

			return result;
		}

		// Строит график для процесса с выбранным диапазоном дат
		public SeriesCollection BuildChart(string processName, DateRange range, out List<string> labels)
		{
			labels = new List<string>();
			var values = new ChartValues<double>();

			// 👇 1. Точное совпадение (быстрая проверка)
			if (_cache.ContainsKey(processName))
			{
				var rawData = _cache[processName];
				var filtered = FilterByRange(rawData, range);

				// 👇 Добавляем недостающие даты с 0
				var completeData = FillMissingDates(filtered, range);

				foreach (var kvp in completeData.OrderBy(x => x.Key))
				{
					values.Add(kvp.Value / 60.0);
					labels.Add(FormatLabel(kvp.Key, range));
				}

				return new SeriesCollection
				{
					new LineSeries
					{
						Values = values,
						Stroke = System.Windows.Media.Brushes.SteelBlue,
						Fill = System.Windows.Media.Brushes.Transparent,
						PointGeometry = DefaultGeometries.Circle,
						PointGeometrySize = 8,
						LineSmoothness = 0.3,
						StrokeThickness = 2,
						LabelPoint = point => $"{point.Y:F2} мин"
					}
				};
			}

			// 👇 2. Поиск без учёта регистра
			var processNameLower = processName.ToLower();
			foreach (var kvp in _cache)
			{
				if (kvp.Key.ToLower() == processNameLower)
				{
					AppLogger.Log($"[Graph] Найдено без учёта регистра: {kvp.Key} == {processName}");

					var rawData = kvp.Value;
					var filtered = FilterByRange(rawData, range);

					// 👇 Добавляем недостающие даты с 0
					var completeData = FillMissingDates(filtered, range);

					foreach (var data in completeData.OrderBy(x => x.Key))
					{
						values.Add(data.Value / 60.0);
						labels.Add(FormatLabel(data.Key, range));
					}

					return new SeriesCollection
					{
						new LineSeries
						{
							Values = values,
							Stroke = System.Windows.Media.Brushes.SteelBlue,
							Fill = System.Windows.Media.Brushes.Transparent,
							PointGeometry = DefaultGeometries.Circle,
							PointGeometrySize = 6,
							LineSmoothness = 0.3,
							StrokeThickness = 2
						}
					};
				}
			}

			// 👇 3. Если не нашли
			AppLogger.Log($"[Graph] НЕ НАЙДЕНО: {processName}");
			return new SeriesCollection();
		}

		// 👇 Заполняет недостающие даты нулями
		private Dictionary<DateTime, int> FillMissingDates(Dictionary<DateTime, int> data, DateRange range)
		{
			var result = new Dictionary<DateTime, int>(data);
			var now = DateTime.Today;

			switch (range)
			{
				case DateRange.Week:
					// Добавляем все дни за последнюю неделю
					for (int i = 6; i >= 0; i--)
					{
						var date = now.AddDays(-i);
						if (!result.ContainsKey(date))
						{
							result[date] = 0;
						}
					}
					break;

				case DateRange.Month:
					// Добавляем все дни за последний месяц
					for (int i = 29; i >= 0; i--)
					{
						var date = now.AddDays(-i);
						if (!result.ContainsKey(date))
						{
							result[date] = 0;
						}
					}
					break;

				case DateRange.Year:
					// Для года добавляем все месяцы
					for (int month = 1; month <= 12; month++)
					{
						var date = new DateTime(now.Year, month, 1);
						if (!result.ContainsKey(date))
						{
							result[date] = 0;
						}
					}
					break;

				case DateRange.All:
				default:
					// Для "всё время" не заполняем пропуски
					break;
			}

			return result;
		}

		// Фильтрует данные по выбранному диапазону
		private Dictionary<DateTime, int> FilterByRange(
			Dictionary<DateTime, int> data,
			DateRange range)
		{
			var now = DateTime.Today;
			var result = new Dictionary<DateTime, int>();

			switch (range)
			{
				case DateRange.Week:
					var weekStart = now.AddDays(-6);
					return data.Where(x => x.Key >= weekStart && x.Key <= now).ToDictionary(x => x.Key, x => x.Value);

				case DateRange.Month:
					var monthStart = now.AddDays(-29);
					return data.Where(x => x.Key >= monthStart && x.Key <= now).ToDictionary(x => x.Key, x => x.Value);

				case DateRange.Year:
					// Группируем по месяцам
					var yearly = new Dictionary<DateTime, int>();
					foreach (var kvp in data.Where(x => x.Key.Year == now.Year || x.Key >= now.AddYears(-1)))
					{
						var monthKey = new DateTime(kvp.Key.Year, kvp.Key.Month, 1);
						if (!yearly.ContainsKey(monthKey))
							yearly[monthKey] = 0;
						yearly[monthKey] += kvp.Value;
					}
					return yearly;

				case DateRange.All:
				default:
					return data;
			}
		}

		// Форматирует подпись для оси X в зависимости от диапазона
		private string FormatLabel(DateTime date, DateRange range)
		{
			switch (range)
			{
				case DateRange.Week:
				case DateRange.Month:
					return date.ToString("dd.MM");
				case DateRange.Year:
					return date.ToString("MMM");
				case DateRange.All:
					return date.ToString("MM.yy");
				default:
					return date.ToString("dd.MM");
			}
		}

		// Форматтер для подписей оси X (для LiveCharts)
		public Func<double, string> GetXLabelFormatter(DateRange range)
		{
			return value =>
			{
				int index = (int)value;
				// Значения возвращаются как индексы, реальные метки задаются через Labels
				return string.Empty;
			};
		}

		// Проверяет, есть ли данные для процесса
		public bool HasData(string processName)
		{
			return _cache.ContainsKey(processName) && _cache[processName].Count > 0;
		}

		// Очищает кэш (при необходимости)
		public void ClearCache()
		{
			_cache.Clear();
		}

		// 👇 Для отладки: получить все имена процессов
		public List<string> GetAllProcessNames()
		{
			return _cache.Keys.ToList();
		}
		// 👇 Получает статистику за сегодня из кэша (без чтения файла)
		public Dictionary<string, int> GetTodayStats()
		{
			var result = new Dictionary<string, int>();
			var today = DateTime.Today;

			foreach (var kvp in _cache)
			{
				if (kvp.Value.ContainsKey(today) && kvp.Value[today] > 0)
				{
					result[kvp.Key] = kvp.Value[today];
				}
			}
			return result;
		}
	}
}