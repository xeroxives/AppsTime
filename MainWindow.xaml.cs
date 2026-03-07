#region USINGS
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AppsTime.Data;
using AppsTime.Helpers;
using AppsTime.Models;
using AppsTime.Parser;
using System.Collections.Concurrent;
using Microsoft.Win32;
using LiveCharts;
using LiveCharts.Wpf;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Label = System.Windows.Controls.Label;
using Point = System.Windows.Point;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
#endregion

namespace AppsTime
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region Notify
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string p = null){PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));}
		#endregion

		#region Consts
		private const int RefreshIntervalMs = 2000;
		private const string TitleNameRu = "AppsTime v1.21";
		private const string TitleNameEn = "AppsTime v1.21";
		#endregion

		#region Private
		private readonly ConcurrentDictionary<string, System.Windows.Media.ImageSource> _iconCache =
			new ConcurrentDictionary<string, System.Windows.Media.ImageSource>();
		private System.Timers.Timer _refreshTimer;
		private ICollectionView _collectionView;
		private ProcessStat _selectedItem;
		private CustomData _customData;
		private CustomColors _customColors;
		private ProcessPathData _processPaths;
		private string _lastProcessListHash_AllTime = "";
		private string _lastProcessListHash_Today = "";
		private bool _isRestoredFromTray = false;
		private System.Windows.Forms.NotifyIcon _notifyIcon;
		private System.Windows.Forms.ContextMenuStrip _trayMenu;
		private DrawGraph _drawGraph;
		private string _currentGraphProcess;
		private string _cachedTotalTime = "00:00:00";
		private bool _isTodayMode = false;
		#endregion

		#region Public
		public ObservableCollection<ProcessStat> AllTimeStats { get; set; } = new ObservableCollection<ProcessStat>();
		public string TotalActivityTime { get; private set; } = "00:00:00";
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			this.Activated += MainWindow_Activated;

			_customColors = CustomColorsManager.Load();
			CustomColorsManager.ApplyToResources(_customColors);

			_customData = CustomDataManager.Load();
			_processPaths = ProcessPathManager.Load();

			ProcessStat.GlobalTimeFormat = _customData.TimeFormat ?? "hh_mm_ss";

			_drawGraph = new DrawGraph();
			_ = _drawGraph.InitializeCacheAsync();

			ToastManager.Initialize(ToastContainer);

			ApplySavedLanguage();
			LoadAllTimeStats();
			InitializeRefreshTimer();
			InitializeTrayIcon();
			UpdateMainWindowBackground();

			if (_customData?.MinimizeOnStart == true && AutoStartManager.IsEnabled())
			{
				Dispatcher.BeginInvoke(new Action(() =>
				{
					MinimizeToTray();
					AppLogger.Log("[Startup] Приложение запущено в трей (MinimizeOnStart)");
				}), System.Windows.Threading.DispatcherPriority.Loaded);
			}
			if (CheckBoxTimeMode != null)
			{
				CheckBoxTimeMode.IsChecked = !_isTodayMode;
			}
		}

		#region Tray
		private void InitializeTrayIcon()
		{
			_trayMenu = new System.Windows.Forms.ContextMenuStrip();

			var restoreItem = new System.Windows.Forms.ToolStripMenuItem("Восстановить");
			restoreItem.Click += (s, e) => RestoreFromTray();

			var exitItem = new System.Windows.Forms.ToolStripMenuItem("Выйти");
			exitItem.Click += (s, e) => ExitApplication();

			_trayMenu.Items.Add(restoreItem);
			_trayMenu.Items.Add(exitItem);

			_notifyIcon = new System.Windows.Forms.NotifyIcon
			{
				Icon = System.Drawing.Icon.ExtractAssociatedIcon(
					System.Windows.Forms.Application.ExecutablePath),
				Text = "AppsTime",
				ContextMenuStrip = _trayMenu,
				Visible = false
			};

			_notifyIcon.MouseDoubleClick += (s, e) =>
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Left)
				{
					RestoreFromTray();
				}
			};
		}

		private void MainWindow_Activated(object sender, EventArgs e)
		{
			if (!_isRestoredFromTray && _notifyIcon != null && _notifyIcon.Visible)
			{
				RestoreFromTray();
				_isRestoredFromTray = true;
			}
		}

		private void UpdateTrayMenuLocalization()
		{
			if (_trayMenu == null || _trayMenu.Items.Count < 2)
				return;

			string lang = _customData.Language ?? "ru";
			_trayMenu.Items[0].Text = (lang == "en") ? "Restore" : "Восстановить";
			_trayMenu.Items[1].Text = (lang == "en") ? "Exit" : "Выйти";
			_notifyIcon.Text = "AppsTime";
		}

		public void MinimizeToTray()
		{
			Hide();
			_notifyIcon.Visible = true;
			AppLogger.Log("[Tray] Свёрнуто в трей");
		}

		public void RestoreFromTray()
		{
			Show();
			WindowState = WindowState.Normal;
			Activate();
			Focus();
			_notifyIcon.Visible = false;
			AppLogger.Log("[Tray] Восстановлено из трея");
		}

		private void ExitApplication()
		{
			_notifyIcon?.Dispose();
			_refreshTimer?.Stop();
			_refreshTimer?.Dispose();
			AppLogger.Log("[App] Завершение работы");
			WpfApplication.Current.Shutdown();
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			if (_customData?.MinimizeOnExit == true)
			{
				e.Cancel = true;
				MinimizeToTray();
				return;
			}
			ExitApplication();
		}
		#endregion

		#region Localization
		private void ApplySavedLanguage()
		{
			string lang = _customData.Language ?? "ru";
			AppLogger.Log($"[Lang] Загружен язык: {lang}");
			ApplyLocalization();
		}

		private void UpdateTotalTimeHeader(string lang)
		{
			if (LabelTotalTimeHeader != null)
			{
				if (_isTodayMode)
				{
					LabelTotalTimeHeader.Content = (lang == "en") ? "Today: " : "Сегодня: ";
				}
				else
				{
					LabelTotalTimeHeader.Content = (lang == "en") ? "Total Activity: " : "Общее время: ";
				}
			}
		}

		public void ApplyLocalization()
		{
			string lang = _customData.Language ?? "ru";
			AppLogger.Log($"[Lang] Применён язык: {lang}");

			var label = FindName("LabelAllTime") as Label;
			if (label != null)
			{
				label.Content = (lang == "en") ? "Total Activity Time: " : "Общее время активности: ";
			}

			Title = (lang == "en") ? TitleNameRu : TitleNameEn;
			UpdateMainLabels(lang);
			UpdateTotalTimeHeader(lang);
			UpdateContextMenu();
			UpdateRightPanel(lang);
			UpdateTrayMenuLocalization();
			UpdateGraphComboBoxLocalization(lang);
			_collectionView?.Refresh();
		}

		private void UpdateGraphComboBoxLocalization(string lang)
		{
			if (ComboBoxDateRange == null) return;

			var items = ComboBoxDateRange.Items.Cast<ComboBoxItem>().ToList();
			if (items.Count >= 4)
			{
				items[0].Content = (lang == "en") ? "Week (by days)" : "Неделя (по дням)";
				items[1].Content = (lang == "en") ? "Month (by days)" : "Месяц (по дням)";
				items[2].Content = (lang == "en") ? "Year (by months)" : "Год (по месяцам)";
				items[3].Content = (lang == "en") ? "All time" : "За всё время";
			}
		}

		private void UpdateMainLabels(string lang) { }

		// 👇 Удалено: ShowLocalizedMessageBox (теперь используем ToastManager)

		private void UpdateMenuItem(MenuItem item, string ruText, string enText)
		{
			if (item != null)
			{
				string lang = _customData.Language ?? "ru";
				item.Header = (lang == "en") ? enText : ruText;
			}
		}
		#endregion

		private string CalculateTotalActivityTime()
		{
			try
			{
				Dictionary<string, int> stats;
				if (_isTodayMode)
				{
					stats = _drawGraph?.GetTodayStats() ?? new Dictionary<string, int>();
				}
				else
				{
					stats = DataParser.GetAllTimeStats();
				}

				int totalSeconds = 0;
				foreach (var kvp in stats)
				{
					if (_customData.ExcludedProcesses.Contains(kvp.Key))
						continue;
					totalSeconds += kvp.Value;
				}

				var time = TimeSpan.FromSeconds(totalSeconds);
				return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[TotalTime] Ошибка расчёта: {ex.Message}");
				return "00:00:00";
			}
		}

		private void UpdateRightPanel(string lang)
		{
			var lblName = FindName("LabelName") as Label;
			var lblTime = FindName("LabelTime") as Label;
			var btnExclude = FindName("ButtonExclude") as Button;
			var btnSave = FindName("ButtonSave") as Button;
			var btnSettings = FindName("ButtonSettings") as Button;

			if (lblName != null) lblName.Content = (lang == "en") ? "Name:" : "Имя:";
			if (lblTime != null) lblTime.Content = (lang == "en") ? "Time (seconds):" : "Время (секунды):";
			if (btnExclude != null) btnExclude.Content = (lang == "en") ? "Exclude" : "Исключить";
			if (btnSave != null) btnSave.Content = (lang == "en") ? "Save" : "Сохранить";
			if (btnSettings != null) btnSettings.Content = "⚙️";
		}

		private void UpdateContextMenu()
		{
			string lang = _customData.Language ?? "ru";

			if (ListBoxContextMenu != null)
			{
				UpdateMenuItem(MenuExclude, "🚫 Исключить", "🚫 Exclude");

				// 👇 ДОБАВЬТЕ ЭТОТ БЛОК:
				if (_selectedItem != null)
				{
					string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
					bool isPinned = _customData.PinnedProcesses.Contains(originalKey);

					string ruText = isPinned ? "📌 Открепить" : "📌 Закрепить";
					string enText = isPinned ? "📌 Unpin" : "📌 Pin";

					UpdateMenuItem(MenuPin, ruText, enText);
				}

				UpdateMenuItem(MenuFileLocation, "📁 Расположение файла", "📁 File location");
				UpdateMenuItem(MenuSelectPath, "🗂️ Указать путь", "🗂️ Select path");
				UpdateMenuItem(MenuCombine, "🔗 Объединить", "🔗 Combine");
				UpdateMenuItem(MenuSetTag, "🏷️ Установить тег", "🏷️ Set tag");
				UpdateMenuItem(MenuResetTime, "🔄 Сбросить время", "🔄 Reset time");
				UpdateMenuItem(MenuCopyName, "📋 Копировать имя", "📋 Copy name");
				UpdateMenuItem(MenuCopyTime, "📋 Копировать время", "📋 Copy time");
			}
			UpdateSelectPathVisibility();
		}

		private void UpdateSelectPathVisibility()
		{
			bool pathWasAutoFixed = false;
			if (MenuSelectPath == null || _selectedItem == null)
				return;

			string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
			string existingPath = ProcessPathManager.GetProcessPath(originalKey);

			// 👇 Показываем пункт если пути нет ИЛИ файл не существует
			bool shouldShow = string.IsNullOrEmpty(existingPath) || !System.IO.File.Exists(existingPath);

			MenuSelectPath.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

			// 👇 Показываем Toast если путь невалиден
			if (!string.IsNullOrEmpty(existingPath) && !System.IO.File.Exists(existingPath))
			{
				// Проверяем, запущен ли процесс сейчас
				var processes = Process.GetProcessesByName(originalKey);
				if (processes.Length > 0 && processes[0].MainModule?.FileName != null)
				{
					// Процесс запущен — путь будет исправлен автоматически при GetProcessIcon
					pathWasAutoFixed = true;
				}

				if (!pathWasAutoFixed)
				{
					ToastManager.Warning("Путь изменён", $"Для \"{_selectedItem.ProcessName}\"");
				}
			}
		}

		public void RefreshTimeFormat()
		{
            ProcessStat.GlobalTimeFormat = _customData.TimeFormat ?? "hh_mm_ss";

			var items = AllTimeStats.ToList();
			AllTimeStats.Clear();
			foreach (var item in items)
			{
				AllTimeStats.Add(item);
			}

			AppLogger.Log($"[Settings] Формат времени обновлён: {ProcessStat.GlobalTimeFormat}");
		}

		private void InitializeRefreshTimer()
		{
			_refreshTimer = new System.Timers.Timer(RefreshIntervalMs);
			_refreshTimer.Elapsed += OnRefreshTimerElapsed;
			_refreshTimer.AutoReset = true;
			_refreshTimer.Enabled = true;

			AppLogger.Log($"[Timer] Запущен таймер обновления ({RefreshIntervalMs}мс)");
		}

		private async void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
		{
			await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
			{
				RefreshProcessList();
				UpdateMainWindowBackground();
			});
		}

		private void RefreshProcessList()
		{
			Dictionary<string, int> stats;
			string currentHash;

			if (_isTodayMode)
			{
                //stats = _drawGraph?.GetTodayStats() ?? new Dictionary<string, int>();
				stats = GetTodayStatsDirect();
                currentHash = GetStatsHash(stats);

				if (currentHash != _lastProcessListHash_Today)
				{
					_lastProcessListHash_Today = currentHash;
					LoadAllTimeStats();
				}
			}
			else
			{
				stats = DataParser.GetAllTimeStats();
				currentHash = GetStatsHash(stats);

				if (currentHash != _lastProcessListHash_AllTime)
				{
					_lastProcessListHash_AllTime = currentHash;
					LoadAllTimeStats();
				}
			}
		}

		private string GetStatsHash(Dictionary<string, int> stats)
		{
			return string.Join(",", stats.Select(x => $"{x.Key}:{x.Value}").OrderBy(x => x));
		}

		public void UpdateMainWindowBackground()
		{
			try
			{
				var colorStart = (Color)ColorConverter.ConvertFromString(_customColors.WindowBackgroundStart);
				var colorEnd = (Color)ColorConverter.ConvertFromString(_customColors.WindowBackgroundEnd);

				var gradientBrush = new LinearGradientBrush(
					colorStart,
					colorEnd,
					new Point(0.5, 0),
					new Point(0.5, 1));

				if (this.Content is Grid mainGrid)
				{
					mainGrid.Background = gradientBrush;
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[MainWindow] Ошибка обновления фона: {ex.Message}");
			}
		}

		#region CheckBoxTimeMode
		private void CheckBoxTimeMode_Checked(object sender, RoutedEventArgs e)
		{
			AppLogger.Log("[DEBUG] CheckBoxTimeMode_Checked вызван");
			_isTodayMode = false;  // ✅ Включено = общее время
			UpdateTotalTimeHeader(_customData?.Language ?? "ru");
			AppLogger.Log("[DEBUG] Вызов LoadAllTimeStats() из Checked");
			LoadAllTimeStats();
			AppLogger.Log("[Mode] Переключено на: Все время");
		}

		private void CheckBoxTimeMode_Unchecked(object sender, RoutedEventArgs e)
		{
			AppLogger.Log("[DEBUG] CheckBoxTimeMode_Unchecked вызван");
			_isTodayMode = true;  // ✅ Выключено = сегодня
			UpdateTotalTimeHeader(_customData?.Language ?? "ru");
			AppLogger.Log("[DEBUG] Вызов LoadAllTimeStats() из Unchecked");
			LoadAllTimeStats();
			AppLogger.Log("[Mode] Переключено на: Сегодня");
		}
		#endregion

		private void LoadAllTimeStats()
		{
			AppLogger.Log($"LoadAllTimeStats() вызван | _isTodayMode={_isTodayMode}", "DEBUG");
			AppLogger.Log($"_drawGraph={(_drawGraph != null ? "OK" : "NULL")}", "DEBUG");

			try
			{
				var selectedItem = ListBoxAllTime.SelectedItem as ProcessStat;
				string selectedOriginalKey = selectedItem?.OriginalKey;

				ListBoxAllTime.SelectionChanged -= ListBoxAllTime_SelectionChanged;

				var newStats = new ObservableCollection<ProcessStat>();

				Dictionary<string, int> stats;
				if (_isTodayMode)
				{
					AppLogger.Log("Запрос GetTodayStats()", "DEBUG");
					stats = _drawGraph?.GetTodayStats() ?? new Dictionary<string, int>();
					AppLogger.Log($"GetTodayStats() вернул {stats.Count} записей", "DEBUG");
				}
				else
				{
					AppLogger.Log("Запрос GetAllTimeStats()", "DEBUG");
					stats = DataParser.GetAllTimeStats();
					AppLogger.Log($"GetAllTimeStats() вернул {stats.Count} записей", "DEBUG");
				}

				ProcessStat restoredItem = null;
				bool pinnedSectionEnded = false;

				foreach (var kvp in stats
					.OrderByDescending(x => _customData.PinnedProcesses.Contains(x.Key))
					.ThenByDescending(x => x.Value))
				{
					if (_customData.ExcludedProcesses.Contains(kvp.Key))
						continue;

					// 👇 Добавляем разделитель после последнего закреплённого процесса
					if (!pinnedSectionEnded && !_customData.PinnedProcesses.Contains(kvp.Key))
					{
						newStats.Add(new ProcessStat { IsSeparator = true });
						pinnedSectionEnded = true;
					}

					int actualTime = kvp.Value;
					int delta = _customData.TimeOverrides.TryGetValue(kvp.Key, out var d) ? d : 0;
					int displayTime = actualTime + delta;

					var processStat = new ProcessStat
					{
						OriginalKey = kvp.Key,
						ProcessName = kvp.Key,
						TotalSeconds = displayTime,
						IsSeparator = false
					};

					processStat.Icon = GetProcessIcon(kvp.Key);

					if (_customData.NameAliases.TryGetValue(kvp.Key, out var alias))
					{
						processStat.ProcessName = alias;
					}

					newStats.Add(processStat);

					if (selectedOriginalKey != null && processStat.OriginalKey == selectedOriginalKey)
					{
						restoredItem = processStat;
					}
				}

				AllTimeStats = newStats;
				DataContext = null;
				DataContext = this;

				ListBoxAllTime.ItemsSource = AllTimeStats;

				_collectionView = CollectionViewSource.GetDefaultView(AllTimeStats);
				_collectionView.Refresh();

				if (restoredItem != null)
				{
					ListBoxAllTime.SelectedItem = restoredItem;
				}

				ListBoxAllTime.SelectionChanged += ListBoxAllTime_SelectionChanged;

				UpdateTotalActivityTime();

				AppLogger.Log($"Список обновлён ({newStats.Count} процессов)", "UI");
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[UI ERROR] Ошибка загрузки статистики: {ex.Message}");
			}
		}

        private Dictionary<string, int> GetTodayStatsDirect()
        {
            var result = new Dictionary<string, int>();
            var today = DateTime.Today;
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "digital-wellbeing",
                "dailylogs"
            );
            var todayFile = Path.Combine(logDirectory, $"{today:MM-dd-yyyy}.log");

            try
            {
                if (File.Exists(todayFile))
                {
                    foreach (var line in File.ReadLines(todayFile))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length < 2)
                            continue;

                        var processName = parts[0].Trim().ToLower().ToString();

                        if (int.TryParse(parts[1], out int seconds) && seconds > 0)
                        {
                            if (!result.ContainsKey(processName))
                                result[processName] = 0;

                            result[processName] += seconds;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[Today] Ошибка чтения: {ex.Message}");
            }
            AppLogger.LogError($"[Today] No errors");
            return result;
        }
        private void UpdateTotalActivityTime()
		{
			try
			{
				_cachedTotalTime = CalculateTotalActivityTime();

				if (TextBlockTotalTime != null)
				{
					TextBlockTotalTime.Text = _cachedTotalTime;
				}

				AppLogger.Log($"[TotalTime] Общее время: {_cachedTotalTime}");
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[TotalTime] Ошибка обновления: {ex.Message}");

				if (TextBlockTotalTime != null)
				{
					TextBlockTotalTime.Text = "00:00:00";
				}
			}
		}

		private System.Windows.Media.ImageSource GetProcessIcon(string processName)
		{
			string processPath = null;

			try
			{
				var processes = Process.GetProcessesByName(processName);
				if (processes.Length > 0)
				{
					foreach (var proc in processes)
					{
						try
						{
							if (proc.MainModule != null)
							{
								processPath = proc.MainModule.FileName;

								if (!string.IsNullOrEmpty(processPath))
								{
									string existingPath = ProcessPathManager.GetProcessPath(processName);

									if (string.IsNullOrEmpty(existingPath) || !System.IO.File.Exists(existingPath))
									{
										ProcessPathManager.UpdateProcessPath(processName, processPath);
										AppLogger.Log($"[Icon] Путь обновлён: {processName} → {processPath}");
									}
								}

								proc.Dispose();
								break;
							}
						}
						catch
						{
							proc.Dispose();
							continue;
						}
					}
				}
			}
			catch
			{
				// Игнорируем ошибки доступа к процессам
			}

			if (string.IsNullOrEmpty(processPath))
			{
				processPath = ProcessPathManager.GetProcessPath(processName);
			}

			if (!string.IsNullOrEmpty(processPath) && System.IO.File.Exists(processPath))
			{
				if (_iconCache.TryGetValue(processPath, out var cachedIcon))
				{
					return cachedIcon;
				}

				try
				{
					var icon = System.Drawing.Icon.ExtractAssociatedIcon(processPath);
					if (icon != null)
					{
						var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
							icon.Handle,
							System.Windows.Int32Rect.Empty,
							System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

						_iconCache.TryAdd(processPath, source);
						icon.Dispose();
						return source;
					}
				}
				catch
				{
					// Ошибка извлечения иконки
				}
			}

			return null;
		}

		protected override void OnClosed(EventArgs e)
		{
			_refreshTimer?.Stop();
			_refreshTimer?.Dispose();
			_notifyIcon?.Dispose();
			AppLogger.Log("[Timer] Таймер остановлен");
			base.OnClosed(e);
		}

		private void ListBoxAllTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ListBoxAllTime.SelectedItem is ProcessStat selected && !selected.IsSeparator)
			{
				_selectedItem = selected;
				TextBoxProcessName.Text = selected.ProcessName;
				TextBoxTimeSeconds.Text = selected.TotalSeconds.ToString();

				ShowGraphForProcess(selected.OriginalKey ?? selected.ProcessName);
			}
			else
			{
				_selectedItem = null;
				TextBoxProcessName.Text = " ";
				TextBoxTimeSeconds.Text = " ";
				HideGraph();
			}

			UpdateSelectPathVisibility();
			UpdateContextMenu();
		}

		#region Graph
		private void ShowGraphForProcess(string processName)
		{
			_currentGraphProcess = processName;

			AppLogger.Log($"[Graph] Запрос графика для: {processName}");
			AppLogger.Log($"[Graph] Есть данные: {_drawGraph?.HasData(processName)}");

			if (_drawGraph != null)
			{
				var allKeys = _drawGraph.GetAllProcessNames();
				AppLogger.Log($"[Graph] Доступные процессы в кэше: {string.Join(", ", allKeys.Take(20))}");
			}

			if (GraphSection != null)
			{
				GraphSection.Visibility = Visibility.Visible;

				if (ComboBoxDateRange.Items.Count > 0 && ComboBoxDateRange.SelectedIndex == -1)
				{
					ComboBoxDateRange.SelectedIndex = 0;
				}

				UpdateGraph();
			}
		}

		private void HideGraph()
		{
			if (GraphSection != null)
			{
				GraphSection.Visibility = Visibility.Collapsed;
			}
		}

		private void ComboBoxDateRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateGraph();
		}

		private void UpdateGraph()
		{
			if (string.IsNullOrEmpty(_currentGraphProcess) || ProcessChart == null || _drawGraph == null)
				return;

			DrawGraph.DateRange range = DrawGraph.DateRange.All;
			if (ComboBoxDateRange.SelectedItem is ComboBoxItem comboItem)
			{
				string tag = comboItem.Tag?.ToString();
				switch (tag)
				{
					case "week": range = DrawGraph.DateRange.Week; break;
					case "month": range = DrawGraph.DateRange.Month; break;
					case "year": range = DrawGraph.DateRange.Year; break;
					case "all": range = DrawGraph.DateRange.All; break;
				}
			}

			var series = _drawGraph.BuildChart(_currentGraphProcess, range, out var labels);

			if (series.Count == 0 || labels.Count == 0)
			{
				ProcessChart.Series = new SeriesCollection();
				GraphStatus.Text = GetText("Нет данных за выбранный период", "No data for selected period");
				GraphStatus.Visibility = Visibility.Visible;
				return;
			}

			ProcessChart.DataTooltip = new LiveCharts.Wpf.DefaultTooltip { ShowSeries = false };

			ProcessChart.Series = series;

			AxisX.Labels = labels.ToArray();
			AxisX.Title = range == DrawGraph.DateRange.Year
				? GetText("Месяц", "Month")
				: GetText("Дата", "Date");

			AxisX.MinValue = -0.1;
			AxisX.MaxValue = labels.Count * 1.01;

			var maxVal = series[0].Values.Cast<double>().Max();
			AxisY.MaxValue = Math.Max(maxVal * 1.1, 50);
			AxisY.Title = GetText("Мин", "Min");

			GraphStatus.Text = $"{GetText("Показано", "Shown")}: {labels.Count} {GetText("точек", "points")} | " +
							   $"{GetText("Всего", "Total")}: {series[0].Values.Cast<double>().Sum():F0} {GetText("мин", "min")}";
			GraphStatus.Visibility = Visibility.Visible;
		}

		private void ButtonCloseGraph_Click(object sender, RoutedEventArgs e)
		{
			HideGraph();
			ListBoxAllTime.SelectedItem = null;
		}
		#endregion

		#region Buttons
		private void ButtonSave_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null)
			{
				ToastManager.Warning("Внимание", GetText("Выберите процесс для редактирования!", "Select a process!"));
				return;
			}

			string newName = TextBoxProcessName.Text.Trim();

			if (string.IsNullOrWhiteSpace(newName))
			{
				ToastManager.Error("Ошибка", GetText("Имя процесса не может быть пустым!", "Process name cannot be empty!"));
				return;
			}

			string originalKey = _selectedItem.OriginalKey;

			if (string.IsNullOrEmpty(originalKey))
			{
				originalKey = _selectedItem.ProcessName;
				foreach (var alias in _customData.NameAliases)
				{
					if (alias.Value == _selectedItem.ProcessName)
					{
						originalKey = alias.Key;
						break;
					}
				}
			}

			if (newName != _selectedItem.ProcessName)
			{
				_customData.NameAliases[originalKey] = newName;
				AppLogger.Log($"[Save] Сохранено имя: {originalKey} → {newName}", "UI");
			}
			else
			{
				AppLogger.Log($"[Save] Имя не изменено: {originalKey}", "UI");
			}

			CustomDataManager.Save(_customData);

			Dispatcher.Invoke(() => LoadAllTimeStats(), System.Windows.Threading.DispatcherPriority.Render);

			ToastManager.Success("Сохранено", "");
			AppLogger.Log($"[UI] Список обновлён", "UI");
		}

		private void ButtonExclude_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null)
			{
				ToastManager.Warning("Внимание", GetText("Выберите процесс для исключения!", "Select a process!"));
				return;
			}

			string lang = _customData.Language ?? "ru";
			string msg = (lang == "en")
				? $"Exclude process \"{_selectedItem.ProcessName}\"?"
				: $"Исключить процесс \"{_selectedItem.ProcessName}\"?";
			string title = (lang == "en") ? "Confirm" : "Подтверждение";

			// 👇 Оставляем MessageBox для подтверждения (Yes/No)
			var result = WpfMessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				string originalKey = _selectedItem.OriginalKey;

				if (string.IsNullOrEmpty(originalKey))
				{
					originalKey = _selectedItem.ProcessName;
					foreach (var alias in _customData.NameAliases)
					{
						if (alias.Value == _selectedItem.ProcessName)
						{
							originalKey = alias.Key;
							break;
						}
					}
				}

				_customData.ExcludedProcesses.Add(originalKey);
				AllTimeStats.Remove(_selectedItem);

				ListBoxAllTime.SelectedItem = null;
				TextBoxProcessName.Text = "";
				TextBoxTimeSeconds.Text = "";
				_selectedItem = null;

				CustomDataManager.Save(_customData);

				AppLogger.Log($"[UI] Исключён и сохранён: {originalKey}");

				Dispatcher.Invoke(() => LoadAllTimeStats(), System.Windows.Threading.DispatcherPriority.Render);

				ToastManager.Success("Исключено", $"\"{_selectedItem.ProcessName}\" удалён из списка");
			}
		}

		private void ButtonSettings_Click(object sender, RoutedEventArgs e)
		{
			string oldLanguage = _customData.Language ?? "ru";

			var settingsWindow = new SettingsWindow(this, _customData, _customColors);
			settingsWindow.ShowDialog();

			if (settingsWindow.UpdatedColors != null)
			{
				_customColors = settingsWindow.UpdatedColors;
			}

			string newLanguage = _customData.Language ?? "ru";
			if (newLanguage != oldLanguage)
			{
				ApplyLocalization();
			}

			UpdateMainWindowBackground();
			LoadAllTimeStats();
		}
        private async void ButtonReload_Click(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку на время перезагрузки
            ButtonReload.IsEnabled = false;

            try
            {
                AppLogger.Log("[Reload] Начало полной перезагрузки данных", "UI");

                _iconCache.Clear();
                AppLogger.Log("[Reload] Кэш иконок очищен", "UI");

                _drawGraph?.ClearCache();
                await _drawGraph.InitializeCacheAsync();
                AppLogger.Log("[Reload] Кэш графика перезагружен", "UI");

                _lastProcessListHash_AllTime = "";
                _lastProcessListHash_Today = "";

                LoadAllTimeStats();
                UpdateGraph();
                UpdateMainWindowBackground();
                //ToastManager.Success("Перезагружено", "Все данные обновлены");

                AppLogger.Log("[Reload] Перезагрузка завершена успешно", "UI");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[Reload] Ошибка перезагрузки: {ex.Message}");
                ToastManager.Error("Ошибка", "Не удалось перезагрузить данные");
            }
            finally
            {
                ButtonReload.IsEnabled = true;
            }
        }
        #endregion

        #region Context Menu
        private void MenuExclude_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;
			ButtonExclude_Click(sender, e);
		}

		private void MenuPin_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
			bool isPinned = _customData.PinnedProcesses.Contains(originalKey);

			if (isPinned)
			{
				// Открепить
				_customData.PinnedProcesses.Remove(originalKey);
				//AppLogger.Log($"[Pin] Откреплён: {originalKey}", "UI");
				//ToastManager.Info("Откреплено", $"\"{_selectedItem.ProcessName}\" больше не закреплён");
			}
			else
			{
				// Закрепить
				_customData.PinnedProcesses.Add(originalKey);
				//AppLogger.Log($"[Pin] Закреплён: {originalKey}", "UI");
				//ToastManager.Success("Закреплено", $"\"{_selectedItem.ProcessName}\" теперь вверху списка");
			}

			CustomDataManager.Save(_customData);
			LoadAllTimeStats();
			UpdateContextMenu();
		}

		private void MenuFileLocation_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null)
			{
				ToastManager.Warning("Внимание", GetText("Выберите процесс!", "Select a process!"));
				return;
			}

			try
			{
				string processName = _selectedItem.ProcessName;
				string originalKey = _selectedItem.OriginalKey ?? processName;
				string processPath = null;

				try
				{
					var processes = Process.GetProcessesByName(originalKey);
					if (processes.Length > 0 && processes[0].MainModule != null)
					{
						processPath = processes[0].MainModule.FileName;
						AppLogger.Log($"[Menu] Получен путь от процесса: {processPath}");
					}
				}
				catch (Exception ex)
				{
					AppLogger.Log($"[Menu] Нет доступа к процессу {originalKey}: {ex.Message}");
				}

				if (string.IsNullOrEmpty(processPath))
				{
					processPath = ProcessPathManager.GetProcessPath(originalKey);
					if (!string.IsNullOrEmpty(processPath))
					{
						AppLogger.Log($"[Menu] Получен путь из paths.json: {processPath}");
					}
				}

				if (!string.IsNullOrEmpty(processPath) && System.IO.File.Exists(processPath))
				{
					Process.Start("explorer.exe", $"/select,\"{processPath}\"");
					AppLogger.Log($"[Menu] Открыт проводник с выделением: {processPath}");
				}
				else if (!string.IsNullOrEmpty(processPath))
				{
					string directory = System.IO.Path.GetDirectoryName(processPath);
					if (!string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory))
					{
						Process.Start("explorer.exe", directory);
						AppLogger.Log($"[Menu] Открыта папка (файл не найден): {directory}");

						ToastManager.Warning(
							"Файл не найден",
							GetText(
								"Файл не найден по сохранённому пути. Открыта папка из last known location.",
								"File not found at saved path. Opened folder from last known location."));
					}
					else
					{
						ToastManager.Error(
							"Путь не найден",
							GetText(
								"Путь к файлу не найден. Процесс никогда не был запущен или путь устарел.",
								"Process path not found. Process was never run or path is outdated."));
					}
				}
				else
				{
					ToastManager.Error(
						"Путь не найден",
						GetText(
							"Путь к файлу не найден. Процесс никогда не был запущен во время работы программы.",
							"Process path not found. Process was never run while this app was active."));
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError($"[Menu] Ошибка открытия проводника: {ex.Message}");
				ToastManager.Error("Ошибка", GetText($"Ошибка: {ex.Message}", $"Error: {ex.Message}"));
			}
		}

		private void MenuSelectPath_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null)
			{
				ToastManager.Warning("Внимание", GetText("Выберите процесс!", "Select a process!"));
				return;
			}

			string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;

			var openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = (_customData.Language == "en") ? "Select executable file" : "Выберите исполняемый файл",
				Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
				CheckFileExists = true,
				CheckPathExists = true
			};

			if (openFileDialog.ShowDialog() == true)
			{
				string selectedPath = openFileDialog.FileName;

				if (ProcessPathManager.SaveUserSelectedPath(originalKey, selectedPath))
				{
					var selectedItem = AllTimeStats.FirstOrDefault(x => x.OriginalKey == originalKey);
					if (selectedItem != null)
					{
						selectedItem.Icon = GetProcessIcon(originalKey);
					}

					if (MenuSelectPath != null)
					{
						MenuSelectPath.Visibility = Visibility.Collapsed;
					}

					AppLogger.Log($"[Menu] Пользователь выбрал путь: {originalKey} → {selectedPath}");
					ToastManager.Success("Путь сохранён", $"Для процесса \"{originalKey}\"");
				}
				else
				{
					ToastManager.Error(
						"Ошибка",
						GetText(
							"Не удалось сохранить путь. Проверьте права доступа к файлу.",
							"Failed to save path. Check file access permissions."));
				}
			}
		}

		private void MenuCombine_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			ToastManager.Info(
				"В разработке",
				GetText(
					$"Объединить \"{_selectedItem.ProcessName}\" с... (Функционал в разработке)",
					$"Combine \"{_selectedItem.ProcessName}\" with... (Feature in development)"));
		}

		private void MenuSetTag_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			ToastManager.Info(
				"В разработке",
				GetText(
					$"Установить тег для \"{_selectedItem.ProcessName}\" (Функционал в разработке)",
					$"Set tag for \"{_selectedItem.ProcessName}\" (Feature in development)"));
		}

		private void MenuResetTime_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			string lang = _customData.Language ?? "ru";
			string msg = (lang == "en")
				? $"Reset time for \"{_selectedItem.ProcessName}\"?\nThis will remove the time override from settings."
				: $"Сбросить время для \"{_selectedItem.ProcessName}\"?\nЭто удалит переопределение времени из настроек.";
			string title = (lang == "en") ? "Reset time" : "Сброс времени";

			// 👇 Оставляем MessageBox для подтверждения (Yes/No)
			var result = WpfMessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
				_customData.TimeOverrides.Remove(originalKey);
				CustomDataManager.Save(_customData);
				LoadAllTimeStats();

				AppLogger.Log($"[Menu] Сброшено время: {originalKey}");
				ToastManager.Success("Сброшено", $"Время для \"{originalKey}\" сброшено");
			}
		}

		private async void MenuCopyName_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			bool success = await ClipboardHelper.SetTextAsync(_selectedItem.ProcessName);

			if (success)
			{
				AppLogger.Log($"[Menu] Скопировано имя: {_selectedItem.ProcessName}");
				ToastManager.Success("Скопировано", $"Имя \"{_selectedItem.ProcessName}\" в буфере");
			}
			else
			{
				ToastManager.Warning(
					"Не удалось скопировать",
					GetText(
						"Буфер обмена занят. Попробуйте ещё раз.",
						"Clipboard is busy. Try again."));
			}
		}

		private async void MenuCopyTime_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedItem == null) return;

			bool success = await ClipboardHelper.SetTextAsync(_selectedItem.TimeFormatted);

			if (success)
			{
				AppLogger.Log($"[Menu] Скопировано время: {_selectedItem.TimeFormatted}");
				ToastManager.Success("Скопировано", $"Время \"{_selectedItem.TimeFormatted}\" в буфере");
			}
			else
			{
				ToastManager.Warning(
					"Не удалось скопировать",
					GetText(
						"Буфер обмена занят. Попробуйте ещё раз.",
						"Clipboard is busy. Try again."));
			}
		}
		#endregion

		#region Helpers
		private string GetText(string ru, string en)
		{
			return (_customData.Language ?? "ru") == "en" ? en : ru;
		}
		#endregion
	}
}