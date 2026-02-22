using AppsTime.Data;
using AppsTime.Helpers;
using AppsTime.Models;
using AppsTime.Parser;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AppsTime
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ProcessStat> AllTimeStats { get; set; }
            = new ObservableCollection<ProcessStat>();

        private System.Timers.Timer _refreshTimer;
        private const int RefreshIntervalMs = 1000;
        private ICollectionView _collectionView;
        private ProcessStat _selectedItem;
        private CustomData _customData;
        private CustomColors _customColors;
        private string _lastProcessListHash = "";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _customColors = CustomColorsManager.Load();
            CustomColorsManager.ApplyToResources(_customColors);

            _customData = CustomDataManager.Load();

            // 👇 Устанавливаем глобальный формат времени
            ProcessStat.GlobalTimeFormat = _customData.TimeFormat ?? "hh_mm_ss";

            // 👇 Применяем сохранённый язык
            ApplySavedLanguage();

            LoadAllTimeStats();
            InitializeRefreshTimer();
            UpdateMainWindowBackground();
        }

        // 👇 Применяем сохранённый язык при запуске
        private void ApplySavedLanguage()
        {
            string lang = _customData.Language ?? "ru";
            AppLogger.Log($"[Lang] Загружен язык: {lang}");

            // Применяем локализацию ко всему UI
            ApplyLocalization();
        }

        // 👇 Обновляем UI при смене языка
        public void ApplyLocalization()
        {
            string lang = _customData.Language ?? "ru";
            AppLogger.Log($"[Lang] Применён язык: {lang}");

            // Обновляем заголовок окна
            Title = (lang == "en") ? "AppsTime" : "AppsTime";

            // Обновляем основные элементы UI
            UpdateMainLabels(lang);

            // Обновляем контекстное меню
            UpdateContextMenu();

            // Обновляем правую панель (метки и кнопки)
            UpdateRightPanel(lang);

            // Принудительно обновляем ListBox для перерисовки времени
            _collectionView?.Refresh();
        }

        // 👇 Обновляет основные метки в главном окне
        private void UpdateMainLabels(string lang)
        {
            // Заголовок списка
            var label = FindName("LabelAllTime") as Label;
            if (label != null)
            {
                label.Content = (lang == "en") ? "All Time:" : "Всё время:";
            }
        }

        // 👇 Обновляет правую панель (метки и кнопки)
        private void UpdateRightPanel(string lang)
        {
            // Метки
            var lblName = FindName("LabelName") as Label;
            var lblTime = FindName("LabelTime") as Label;
            var lblFormatted = FindName("LabelFormatted") as Label;

            if (lblName != null) lblName.Content = (lang == "en") ? "Name:" : "Имя:";
            if (lblTime != null) lblTime.Content = (lang == "en") ? "Time (seconds):" : "Время (секунды):";
            if (lblFormatted != null) lblFormatted.Content = (lang == "en") ? "Formatted:" : "Форматировано:";

            // Кнопки
            var btnExclude = FindName("ButtonExclude") as Button;
            var btnSave = FindName("ButtonSave") as Button;
            var btnSettings = FindName("ButtonSettings") as Button;

            if (btnExclude != null) btnExclude.Content = (lang == "en") ? "Exclude" : "Исключить";
            if (btnSave != null) btnSave.Content = (lang == "en") ? "Save" : "Сохранить";
            if (btnSettings != null) btnSettings.Content = (lang == "en") ? "⚙️ Settings" : "⚙️ Настройки";
        }

        // 👇 Обновляет текст пунктов контекстного меню
        private void UpdateContextMenu()
        {
            string lang = _customData.Language ?? "ru";

            if (ListBoxContextMenu != null)
            {
                UpdateMenuItem(MenuExclude, "🚫 Исключить", "🚫 Exclude");
                UpdateMenuItem(MenuCombine, "🔗 Объединить", "🔗 Combine");
                UpdateMenuItem(MenuSetTag, "🏷️ Установить тег", "🏷️ Set tag");
                UpdateMenuItem(MenuResetTime, "🔄 Сбросить время", "🔄 Reset time");
                UpdateMenuItem(MenuCopyName, "📋 Копировать имя", "📋 Copy name");
                UpdateMenuItem(MenuCopyTime, "📋 Копировать время", "📋 Copy time");
            }
        }

        private void UpdateMenuItem(MenuItem item, string ruText, string enText)
        {
            if (item != null)
            {
                string lang = _customData.Language ?? "ru";
                item.Header = (lang == "en") ? enText : ruText;
            }
        }

        // 👇 Helper для локализации MessageBox
        private void ShowLocalizedMessageBox(string msgRu, string msgEn, string titleRu, string titleEn,
            MessageBoxButton buttons, MessageBoxImage icon)
        {
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en") ? msgEn : msgRu;
            string title = (lang == "en") ? titleEn : titleRu;
            MessageBox.Show(msg, title, buttons, icon);
        }

        public void RefreshTimeFormat()
        {
            ProcessStat.GlobalTimeFormat = _customData.TimeFormat ?? "hh_mm_ss";

            // Обновляем коллекцию (триггерим PropertyChanged)
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
            // Обновляем UI через Dispatcher (требуется для WPF)
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RefreshProcessList();
                UpdateMainWindowBackground();
            });
        }

        private void RefreshProcessList()
        {
            var stats = DataParser.GetAllTimeStats();
            string currentHash = GetStatsHash(stats);

            // Обновляем только если данные изменились
            if (currentHash != _lastProcessListHash)
            {
                _lastProcessListHash = currentHash;
                LoadAllTimeStats();
            }
        }

        private string GetStatsHash(Dictionary<string, int> stats)
        {
            // Простая хеш-сумма для проверки изменений
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

                // Находим главный Grid и применяем фон
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

                    // 👇 Фактическое время из лога
                    int actualTime = kvp.Value;

                    // 👇 Получаем дельту по ОРИГИНАЛЬНОМУ ключу
                    int delta = _customData.TimeOverrides.TryGetValue(kvp.Key, out var d) ? d : 0;

                    // 👇 Отображаемое время = фактическое + дельта
                    int displayTime = actualTime + delta;

                    var processStat = new ProcessStat
                    {
                        OriginalKey = kvp.Key,
                        ProcessName = kvp.Key,
                        TotalSeconds = displayTime
                    };

                    // Применяем алиас для отображения
                    if (_customData.NameAliases.TryGetValue(kvp.Key, out var alias))
                    {
                        processStat.ProcessName = alias;
                    }

                    newStats.Add(processStat);

                    // Восстанавливаем выделение по оригинальному ключу
                    if (selectedOriginalKey != null && processStat.OriginalKey == selectedOriginalKey)
                    {
                        restoredItem = processStat;
                    }
                    AppLogger.Log($"[Debug] {kvp.Key} | Лог: {kvp.Value}s | Дельта: {delta}s | Отображение: {displayTime}s");
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

                AppLogger.Log($"[UI] Список обновлён ({newStats.Count} процессов)");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UI] Ошибка загрузки статистики: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
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
                TextBlockTimeFormatted.Text = selected.TimeFormatted;
            }
            else
            {
                _selectedItem = null;
                TextBoxProcessName.Text = "";
                TextBoxTimeSeconds.Text = "";
                TextBlockTimeFormatted.Text = "";
            }
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

            // 👇 Используем OriginalKey (оригинальное имя из лога)
            string originalKey = _selectedItem.OriginalKey;

            // Если OriginalKey не установлен, пытаемся найти через алиасы
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

            // Парсим введённое время
            int userInputTime = 0;
            if (int.TryParse(TextBoxTimeSeconds.Text, out int parsedTime))
            {
                userInputTime = Math.Max(0, parsedTime);
            }

            // 👇 Получаем ФАКТИЧЕСКОЕ время из лога (не из UI!)
            var stats = DataParser.GetAllTimeStats();
            int actualTimeFromLog = stats.TryGetValue(originalKey, out var t) ? t : 0;

            // 👇 Вычисляем дельту: введённое - фактическое
            int delta = userInputTime - actualTimeFromLog;

            // Сохраняем алиас (если изменили имя)
            if (newName != _selectedItem.ProcessName)
            {
                _customData.NameAliases[originalKey] = newName;
            }

            // 👇 Сохраняем ДЕЛЬТУ по оригинальному ключу
            _customData.TimeOverrides[originalKey] = delta;

            // Автосохранение
            CustomDataManager.Save(_customData);

            // 👇 МГНОВЕННОЕ обновление списка
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

            var result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 👇 Используем OriginalKey
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
                TextBlockTimeFormatted.Text = "";
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
            // 👇 Сохраняем текущий язык для сравнения
            string oldLanguage = _customData.Language ?? "ru";

            var settingsWindow = new SettingsWindow(this, _customData, _customColors);
            settingsWindow.ShowDialog();

            // 👈 Получаем обновлённые цвета из SettingsWindow
            if (settingsWindow.UpdatedColors != null)
            {
                _customColors = settingsWindow.UpdatedColors;
            }

            // 👇 Если язык изменился — применяем локализацию
            string newLanguage = _customData.Language ?? "ru";
            if (newLanguage != oldLanguage)
            {
                ApplyLocalization();
            }

            // Обновляем фон главного окна
            UpdateMainWindowBackground();

            LoadAllTimeStats();
        }

        #region Context Menu

        private void MenuExclude_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            ButtonExclude_Click(sender, e);
        }

        private void MenuCombine_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Combine \"{_selectedItem.ProcessName}\" with...\n\n(Feature in development)"
                : $"Объединить \"{_selectedItem.ProcessName}\" с...\n\n(Функционал в разработке)";
            string title = (lang == "en") ? "Combine" : "Объединить";

            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuSetTag_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Set tag for \"{_selectedItem.ProcessName}\"\n\n(Feature in development)"
                : $"Установить тег для \"{_selectedItem.ProcessName}\"\n\n(Функционал в разработке)";
            string title = (lang == "en") ? "Tag" : "Тег";

            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuResetTime_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            string lang = _customData.Language ?? "ru";
            string msg = (lang == "en")
                ? $"Reset time for \"{_selectedItem.ProcessName}\"?\n\nThis will remove the time override from settings."
                : $"Сбросить время для \"{_selectedItem.ProcessName}\"?\n\nЭто удалит переопределение времени из настроек.";
            string title = (lang == "en") ? "Reset time" : "Сброс времени";

            var result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

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
    }
}