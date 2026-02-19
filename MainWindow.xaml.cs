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
        public ObservableCollection<ProcessStat> AllTimeStats { get; }
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
            LoadAllTimeStats();

            // Запускаем таймер
            InitializeRefreshTimer();

            UpdateMainWindowBackground();
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
                // 👇 Сохраняем текущее выделение
                var selectedItem = ListBoxAllTime.SelectedItem as ProcessStat;
                string selectedProcessName = selectedItem?.ProcessName;

                AppLogger.Log($"[UI] Сохранено выделение: {selectedProcessName}");

                // Временно отключаем события
                ListBoxAllTime.SelectionChanged -= ListBoxAllTime_SelectionChanged;

                // Очищаем и загружаем заново
                AllTimeStats.Clear();
                var stats = DataParser.GetAllTimeStats();

                ProcessStat restoredItem = null;

                foreach (var kvp in stats.OrderByDescending(x => x.Value))
                {
                    if (_customData.ExcludedProcesses.Contains(kvp.Key))
                    {
                        continue;
                    }

                    var processStat = new ProcessStat
                    {
                        ProcessName = kvp.Key,
                        TotalSeconds = kvp.Value
                    };

                    if (_customData.NameAliases.TryGetValue(kvp.Key, out var alias))
                    {
                        processStat.ProcessName = alias;
                    }

                    if (_customData.TimeOverrides.TryGetValue(kvp.Key, out var overrideTime))
                    {
                        processStat.TotalSeconds = overrideTime;
                    }

                    AllTimeStats.Add(processStat);

                    // 👇 Ищем элемент для восстановления выделения
                    if (selectedProcessName != null &&
                        (processStat.ProcessName == selectedProcessName ||
                         kvp.Key == selectedProcessName))
                    {
                        restoredItem = processStat;
                    }
                }

                _collectionView?.Refresh();

                // 👇 Восстанавливаем выделение
                if (restoredItem != null)
                {
                    ListBoxAllTime.SelectedItem = restoredItem;
                    AppLogger.Log($"[UI] Восстановлено выделение: {restoredItem.ProcessName}");
                }

                // Возвращаем обработчик
                ListBoxAllTime.SelectionChanged += ListBoxAllTime_SelectionChanged;
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
                MessageBox.Show("Выберите процесс для редактирования!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newName = TextBoxProcessName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя процесса не может быть пустым!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Находим оригинальное имя
            string originalName = _selectedItem.ProcessName;
            foreach (var alias in _customData.NameAliases)
            {
                if (alias.Value == _selectedItem.ProcessName)
                {
                    originalName = alias.Key;
                    break;
                }
            }

            // Проверяем дубликаты
            var existingProcess = AllTimeStats.FirstOrDefault(x =>
                x.ProcessName.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                x != _selectedItem);

            if (existingProcess != null)
            {
                var result = MessageBox.Show(
                    $"Процесс \"{newName}\" уже существует!\n\nОбъединить время?",
                    "Дубликат процесса",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    existingProcess.TotalSeconds += _selectedItem.TotalSeconds;

                    _customData.TimeOverrides[originalName] = existingProcess.TotalSeconds;
                    if (_customData.NameAliases.ContainsKey(originalName))
                        _customData.NameAliases.Remove(originalName);

                    AllTimeStats.Remove(_selectedItem);
                    _collectionView?.Refresh();

                    // 👇 Автосохранение
                    CustomDataManager.Save(_customData);

                    AppLogger.Log($"[UI] Объединено и сохранено");
                    MessageBox.Show($"Процессы объединены!", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    return;
                }
            }

            if (int.TryParse(TextBoxTimeSeconds.Text, out int newTime))
            {
                if (newName != _selectedItem.ProcessName)
                {
                    _customData.NameAliases[originalName] = newName;
                }

                _customData.TimeOverrides[originalName] = newTime;

                _selectedItem.ProcessName = newName;
                _selectedItem.TotalSeconds = newTime;
                _collectionView?.Refresh();

                // 👇 Автосохранение
                CustomDataManager.Save(_customData);

                AppLogger.Log($"[UI] Сохранено: {newName} = {newTime}s");
                MessageBox.Show($"Данные обновлены!\n\nИмя: {newName}\nВремя: {newTime} секунд ({_selectedItem.TimeFormatted})",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Неверный формат времени!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonExclude_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null)
            {
                MessageBox.Show("Выберите процесс для исключения!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Исключить процесс \"{_selectedItem.ProcessName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string originalName = _selectedItem.ProcessName;
                foreach (var alias in _customData.NameAliases)
                {
                    if (alias.Value == _selectedItem.ProcessName)
                    {
                        originalName = alias.Key;
                        break;
                    }
                }

                _customData.ExcludedProcesses.Add(originalName);

                AllTimeStats.Remove(_selectedItem);
                ListBoxAllTime.SelectedItem = null;
                TextBoxProcessName.Text = "";
                TextBoxTimeSeconds.Text = "";
                TextBlockTimeFormatted.Text = "";
                _selectedItem = null;

                // 👇 Автосохранение
                CustomDataManager.Save(_customData);

                AppLogger.Log($"[UI] Исключён и сохранён: {originalName}");
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
            var settingsWindow = new SettingsWindow(this, _customData, _customColors);
            settingsWindow.ShowDialog();

            // 👈 Получаем обновлённые цвета из SettingsWindow
            if (settingsWindow.UpdatedColors != null)
            {
                _customColors = settingsWindow.UpdatedColors;
            }

            // Обновляем фон главного окна
            UpdateMainWindowBackground();

            LoadAllTimeStats();
        }

    }
}