#region USINGS
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
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

            // 👇 Инициализация графика
            _drawGraph = new DrawGraph();
            _ = _drawGraph.InitializeCacheAsync();

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
        }

        #region Consts
        private const int RefreshIntervalMs = 1000;
        private const string TitleNameRu = "AppsTime v1.2";
        private const string TitleNameEn = "AppsTime v1.2";
        #endregion

        #region Private
        private readonly ConcurrentDictionary<string,System.Windows.Media.ImageSource> _iconCache = 
                     new ConcurrentDictionary<string,System.Windows.Media.ImageSource>();
        private System.Timers.Timer _refreshTimer;
        private ICollectionView _collectionView;
        private ProcessStat _selectedItem;
        private CustomData _customData;
        private CustomColors _customColors;
        private ProcessPathData _processPaths;
        private string _lastProcessListHash = "";
        private bool _isRestoredFromTray = false;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private System.Windows.Forms.ContextMenuStrip _trayMenu;
        private DrawGraph _drawGraph;
        private string _currentGraphProcess;
        private string _cachedTotalTime = "00:00:00";
        #endregion

        #region Notify
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

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
                LabelTotalTimeHeader.Content = (lang == "en") ? "Total Activity Time: " : "Общее время активности: ";
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
        private void UpdateMainLabels(string lang) { }// Пусто, если не нужно
        private void ShowLocalizedMessageBox(string msgRu, string msgEn, string titleRu, string titleEn, MessageBoxButton buttons, MessageBoxImage icon)
        {
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en") ? msgEn : msgRu;
            string title = (lang == "en") ? titleEn : titleRu;
            WpfMessageBox.Show(msg, title, buttons, icon);
        }
        private void UpdateMenuItem(MenuItem item, string ruText, string enText)
        {
            if (item != null)
            {
                string lang = _customData.Language ?? "ru";
                item.Header = (lang == "en") ? enText : ruText;
            }
        }
        #endregion
        public ObservableCollection<ProcessStat> AllTimeStats { get; set; }=new ObservableCollection<ProcessStat>();

        public string TotalActivityTime { get; private set; } = "00:00:00";

        private string CalculateTotalActivityTime()
        {
            try
            {
                var stats = DataParser.GetAllTimeStats();
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
            #region Vars
            var lblName = FindName("LabelName") as Label;
            var lblTime = FindName("LabelTime") as Label;
            var btnExclude = FindName("ButtonExclude") as Button;
            var btnSave = FindName("ButtonSave") as Button;
            var btnSettings = FindName("ButtonSettings") as Button;
            #endregion
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
            if (MenuSelectPath == null || _selectedItem == null)
                return;

            string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
            string existingPath = ProcessPathManager.GetProcessPath(originalKey);

            MenuSelectPath.Visibility = string.IsNullOrEmpty(existingPath)
                ? Visibility.Visible
                : Visibility.Collapsed;
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
            var stats = DataParser.GetAllTimeStats();
            string currentHash = GetStatsHash(stats);

            if (currentHash != _lastProcessListHash)
            {
                _lastProcessListHash = currentHash;
                LoadAllTimeStats();
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

        private void LoadAllTimeStats()
        {
            try
            {
                var selectedItem = ListBoxAllTime.SelectedItem as ProcessStat;
                string selectedOriginalKey = selectedItem?.OriginalKey;

                ListBoxAllTime.SelectionChanged -= ListBoxAllTime_SelectionChanged;

                var newStats = new ObservableCollection<ProcessStat>();
                var stats = DataParser.GetAllTimeStats();

                ProcessStat restoredItem = null;

                foreach (var kvp in stats.OrderByDescending(x => x.Value))
                {
                    if (_customData.ExcludedProcesses.Contains(kvp.Key))
                    {
                        continue;
                    }

                    int actualTime = kvp.Value;
                    int delta = _customData.TimeOverrides.TryGetValue(kvp.Key, out var d) ? d : 0;
                    int displayTime = actualTime + delta;

                    var processStat = new ProcessStat
                    {
                        OriginalKey = kvp.Key,
                        ProcessName = kvp.Key,
                        TotalSeconds = displayTime
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

                AppLogger.Log($"[UI] Список обновлён ({newStats.Count} процессов)");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UI] Ошибка загрузки статистики: {ex.Message}");
            }
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
                                    if (string.IsNullOrEmpty(existingPath))
                                    {
                                        ProcessPathManager.UpdateProcessPath(processName, processPath);
                                        AppLogger.Log($"[Icon] Автоматически сохранён путь: {processName}");
                                    }
                                }

                                proc.Dispose();
                                break;
                            }
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            proc.Dispose();
                            continue;
                        }
                        catch (InvalidOperationException)
                        {
                            proc.Dispose();
                            continue;
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
            if (ListBoxAllTime.SelectedItem is ProcessStat selected)
            {
                _selectedItem = selected;
                TextBoxProcessName.Text = selected.ProcessName;
                TextBoxTimeSeconds.Text = selected.TotalSeconds.ToString();

                // 👇 Показываем график для выбранного процесса
                ShowGraphForProcess(selected.OriginalKey ?? selected.ProcessName);
            }
            else
            {
                _selectedItem = null;
                TextBoxProcessName.Text = "";
                TextBoxTimeSeconds.Text = "";

                HideGraph();
            }

            UpdateSelectPathVisibility();
        }

        // 👇 Методы для работы с графиком

        private void ShowGraphForProcess(string processName)
        {
            _currentGraphProcess = processName;

            AppLogger.Log($"[Graph] Запрос графика для: {processName}");
            AppLogger.Log($"[Graph] Есть данные: {_drawGraph?.HasData(processName)}");

            // 👇 Вывести все ключи в кэше для отладки
            if (_drawGraph != null)
            {
                var allKeys = _drawGraph.GetAllProcessNames();
                AppLogger.Log($"[Graph] Доступные процессы в кэше: {string.Join(", ", allKeys.Take(20))}");
            }

            if (GraphSection != null)
            {
                GraphSection.Visibility = Visibility.Visible;
                //GraphTitle.Text = $"График: {processName}";

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

            // Определяем выбранный диапазон
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

            // Строим график
            var series = _drawGraph.BuildChart(_currentGraphProcess, range, out var labels);

            if (series.Count == 0 || labels.Count == 0)
            {
                ProcessChart.Series = new SeriesCollection();
                GraphStatus.Text = GetText("Нет данных за выбранный период", "No data for selected period");
                GraphStatus.Visibility = Visibility.Visible;
                return;
            }
            #region Custom_Tooltip
            ProcessChart.DataTooltip = new LiveCharts.Wpf.DefaultTooltip
            {
                ShowSeries = false
            };
            //var tooltip = new LiveCharts.Wpf.DefaultTooltip
            //{
            //    SelectionMode = LiveCharts.SelectionMode.Single,
            //    ShowSeries = false
            //};
            //tooltip.DataPointHovered += (s, e) =>
            //{
            //    if (e.ChartPoint != null && e.ChartPoint.SeriesView != null)
            //    {
            //        var lineSeries = e.ChartPoint.SeriesView as LiveCharts.Wpf.LineSeries;
            //        if (lineSeries != null)
            //        {
            //            // Форматируем значение
            //            var minutes = e.ChartPoint.Y;
            //            var dateTime = labels[e.ChartPoint.Index];

            //            // Добавляем год к дате
            //            string formattedDate = AddYearToDate(dateTime, range);

            //            // Устанавливаем кастомный текст
            //            lineSeries.DataLabels = true;
            //            lineSeries.LabelPoint = point => $"{formattedDate}\n{minutes:F2} мин";
            //        }
            //    }
            //};
            
            #endregion

            // Применяем данные к графику
            ProcessChart.Series = series;

            // Настраиваем ось X
            AxisX.Labels = labels.ToArray();
            AxisX.Title = range == DrawGraph.DateRange.Year
                ? GetText("Месяц", "Month")
                : GetText("Дата", "Date");

            // Настраиваем ось Y
            var maxVal = series[0].Values.Cast<double>().Max();
            AxisY.MaxValue = Math.Ceiling(maxVal / 50) * 50;
            AxisY.Title = GetText("Мин", "Min");

            // Статус
            GraphStatus.Text = $"{GetText("Показано", "Shown")}: {labels.Count} {GetText("точек", "points")} | " +
                               $"{GetText("Всего", "Total")}: {series[0].Values.Cast<double>().Sum():F0} {GetText("мин", "min")}";
            GraphStatus.Visibility = Visibility.Visible;
        }
        private void ButtonCloseGraph_Click(object sender, RoutedEventArgs e)
        {
            HideGraph();
            ListBoxAllTime.SelectedItem = null;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                ShowLocalizedMessageBox(
                    "Выберите процесс для редактирования!", "Select a process!",
                    "Внимание", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newName = TextBoxProcessName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowLocalizedMessageBox(
                    "Имя процесса не может быть пустым!", "Process name cannot be empty!",
                    "Ошибка", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

            int userInputTime = 0;
            if (int.TryParse(TextBoxTimeSeconds.Text, out int parsedTime))
            {
                userInputTime = Math.Max(0, parsedTime);
            }

            var stats = DataParser.GetAllTimeStats();
            int actualTimeFromLog = stats.TryGetValue(originalKey, out var t) ? t : 0;
            int delta = userInputTime - actualTimeFromLog;

            if (newName != _selectedItem.ProcessName)
            {
                _customData.NameAliases[originalKey] = newName;
            }

            _customData.TimeOverrides[originalKey] = delta;
            CustomDataManager.Save(_customData);

            Dispatcher.Invoke(() => LoadAllTimeStats(),
                System.Windows.Threading.DispatcherPriority.Render);

            AppLogger.Log($"[UI] Сохранено: {newName} | Ключ: {originalKey} | Фактическое: {actualTimeFromLog}s | Введённое: {userInputTime}s | Дельта: {delta}s");
        }

        private void ButtonExclude_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                ShowLocalizedMessageBox(
                    "Выберите процесс для исключения!", "Select a process!",
                    "Внимание", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Exclude process \"{_selectedItem.ProcessName}\"?"
                : $"Исключить процесс \"{_selectedItem.ProcessName}\"?";
            string title = (lang == "en") ? "Confirm" : "Подтверждение";

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

                Dispatcher.Invoke(() => LoadAllTimeStats(),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void ButtonExcludedApps_Click(object sender, RoutedEventArgs e)
        {
            var excludedWindow = new ExcludedAppsWindow(this, _customData);
            excludedWindow.ShowDialog();
            LoadAllTimeStats();
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

        #region Context Menu

        private void MenuExclude_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            ButtonExclude_Click(sender, e);
        }

        private void MenuFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                ShowLocalizedMessageBox(
                    "Выберите процесс!", "Select a process!",
                    "Внимание", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

                        ShowLocalizedMessageBox(
                            "Файл не найден по сохранённому пути.\n\nОткрыта папка из last known location.",
                            "File not found at saved path.\n\nOpened folder from last known location.",
                            "Предупреждение", "Warning",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        ShowLocalizedMessageBox(
                            "Путь к файлу не найден.\n\nПроцесс никогда не был запущен или путь устарел.",
                            "Process path not found.\n\nProcess was never run or path is outdated.",
                            "Ошибка", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ShowLocalizedMessageBox(
                        "Путь к файлу не найден.\n\nПроцесс никогда не был запущен во время работы программы.",
                        "Process path not found.\n\nProcess was never run while this app was active.",
                        "Ошибка", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[Menu] Ошибка открытия проводника: {ex.Message}");
                ShowLocalizedMessageBox(
                    $"Ошибка: {ex.Message}",
                    $"Error: {ex.Message}",
                    "Ошибка", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuSelectPath_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                ShowLocalizedMessageBox(
                    "Выберите процесс!", "Select a process!",
                    "Внимание", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                }
                else
                {
                    ShowLocalizedMessageBox(
                        "Не удалось сохранить путь.\n\nПроверьте права доступа к файлу.",
                        "Failed to save path.\n\nCheck file access permissions.",
                        "Ошибка", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuCombine_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Combine \"{_selectedItem.ProcessName}\" with...\n\n(Feature in development)"
                : $"Объединить \"{_selectedItem.ProcessName}\" с...\n\n(Функционал в разработке)";
            string title = (lang == "en") ? "Combine" : "Объединить";

            WpfMessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuSetTag_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Set tag for \"{_selectedItem.ProcessName}\"\n\n(Feature in development)"
                : $"Установить тег для \"{_selectedItem.ProcessName}\"\n\n(Функционал в разработке)";
            string title = (lang == "en") ? "Tag" : "Тег";

            WpfMessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuResetTime_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Reset time for \"{_selectedItem.ProcessName}\"?\n\nThis will remove the time override from settings."
                : $"Сбросить время для \"{_selectedItem.ProcessName}\"?\n\nЭто удалит переопределение времени из настроек.";
            string title = (lang == "en") ? "Reset time" : "Сброс времени";

            var result = WpfMessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string originalKey = _selectedItem.OriginalKey ?? _selectedItem.ProcessName;
                _customData.TimeOverrides.Remove(originalKey);
                CustomDataManager.Save(_customData);
                LoadAllTimeStats();
                AppLogger.Log($"[Menu] Сброшено время: {originalKey}");
            }
        }

        private async void MenuCopyName_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            bool success = await ClipboardHelper.SetTextAsync(_selectedItem.ProcessName);

            if (success)
            {
                AppLogger.Log($"[Menu] Скопировано имя: {_selectedItem.ProcessName}");
            }
            else
            {
                ShowLocalizedMessageBox(
                    "Не удалось скопировать в буфер обмена.\n\nВозможно, буфер занят другим приложением.\n\nПопробуйте ещё раз.",
                    "Failed to copy to clipboard.\n\nMaybe clipboard is busy.\n\nTry again.",
                    "Ошибка", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void MenuCopyTime_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            bool success = await ClipboardHelper.SetTextAsync(_selectedItem.TimeFormatted);

            if (success)
            {
                AppLogger.Log($"[Menu] Скопировано время: {_selectedItem.TimeFormatted}");
            }
            else
            {
                ShowLocalizedMessageBox(
                    "Не удалось скопировать в буфер обмена.\n\nВозможно, буфер занят другим приложением.\n\nПопробуйте ещё раз.",
                    "Failed to copy to clipboard.\n\nMaybe clipboard is busy.\n\nTry again.",
                    "Ошибка", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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