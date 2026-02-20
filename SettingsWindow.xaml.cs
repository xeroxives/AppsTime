using AppsTime.Data;
using AppsTime.Helpers;
using AppsTime.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AppsTime
{
    public partial class SettingsWindow : Window
    {
        private readonly CustomData _customData;
        private readonly MainWindow _mainWindow;
        private CustomColors _currentColors;
        private ObservableCollection<string> _excludedList;
        public CustomColors UpdatedColors { get; private set; }

        public SettingsWindow(MainWindow owner, CustomData customData, CustomColors colors)
        {
            InitializeComponent();
            Owner = owner;
            _customData = customData;
            _mainWindow = owner;
            _currentColors = colors;
            UpdatedColors = colors;

            LoadColors();
            LoadExcludedApps();
            LoadGeneralSettings();
        }

        #region Colors

        private void LoadColors()
        {
            // 👇 Используем _currentColors (загруженные из файла), а не AppColors

            // Фон
            TextBoxWindowBackgroundStart.Text = _currentColors.WindowBackgroundStart;
            BorderWindowBackgroundStart.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.WindowBackgroundStart));

            TextBoxWindowBackgroundEnd.Text = _currentColors.WindowBackgroundEnd;
            BorderWindowBackgroundEnd.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.WindowBackgroundEnd));

            TextBoxSelectedBackground.Text = _currentColors.SelectedBackground;
            BorderSelectedBackground.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.SelectedBackground));

            TextBoxRunningProcessText.Text = _currentColors.RunningProcessTextColor;
            BorderRunningProcessText.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.RunningProcessTextColor));

            // Кнопки
            TextBoxButtonSave.Text = _currentColors.ButtonSave;
            BorderButtonSave.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonSave));

            TextBoxButtonExclude.Text = _currentColors.ButtonExclude;
            BorderButtonExclude.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonExclude));

            TextBoxButtonInfo.Text = _currentColors.ButtonInfo;
            BorderButtonInfo.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonInfo));

            // Текст
            TextBoxTextPrimary.Text = _currentColors.TextPrimary;
            BorderTextPrimary.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.TextPrimary));

            TextBoxTextSecondary.Text = _currentColors.TextSecondary;
            BorderTextSecondary.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.TextSecondary));
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                UpdatePreview(textBox);
            }
        }

        private void UpdatePreview(TextBox textBox)
        {
            try
            {
                string hex = textBox.Text.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                    var color = Color.FromRgb(r, g, b);
                    var brush = new SolidColorBrush(color);

                    // Находим соответствующий Border
                    string borderName = textBox.Name.Replace("TextBox", "Border");
                    var border = FindName(borderName) as Border;
                    if (border != null)
                    {
                        border.Background = brush;
                    }
                }
            }
            catch
            {
                // Неверный формат цвета
            }
        }

        private void ButtonApplyColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyColor("WindowBackgroundStart", TextBoxWindowBackgroundStart.Text);
                ApplyColor("WindowBackgroundEnd", TextBoxWindowBackgroundEnd.Text);
                ApplyColor("SelectedBackground", TextBoxSelectedBackground.Text);
                ApplyColor("ButtonSave", TextBoxButtonSave.Text);
                ApplyColor("ButtonExclude", TextBoxButtonExclude.Text);
                ApplyColor("ButtonInfo", TextBoxButtonInfo.Text);
                ApplyColor("TextPrimary", TextBoxTextPrimary.Text);
                ApplyColor("TextSecondary", TextBoxTextSecondary.Text);
                ApplyColor("RunningProcessTextColor", TextBoxRunningProcessText.Text);

                // Обновляем _currentColors
                _currentColors.WindowBackgroundStart = TextBoxWindowBackgroundStart.Text;
                _currentColors.WindowBackgroundEnd = TextBoxWindowBackgroundEnd.Text;
                _currentColors.SelectedBackground = TextBoxSelectedBackground.Text;
                _currentColors.ButtonSave = TextBoxButtonSave.Text;
                _currentColors.ButtonExclude = TextBoxButtonExclude.Text;
                _currentColors.ButtonInfo = TextBoxButtonInfo.Text;
                _currentColors.TextPrimary = TextBoxTextPrimary.Text;
                _currentColors.TextSecondary = TextBoxTextSecondary.Text;
                _currentColors.RunningProcessTextColor = TextBoxRunningProcessText.Text;

                // 👈 Обновляем UpdatedColors для передачи в MainWindow
                UpdatedColors = _currentColors;

                // Сохраняем
                if (CustomColorsManager.Save(_currentColors))
                {
                    MessageBox.Show("Цвета применены и сохранены!", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyColor(string resourceName, string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var brush = new SolidColorBrush(color);

                if (Application.Current.Resources.Contains(resourceName))
                {
                    Application.Current.Resources[resourceName] = color;
                }

                var brushName = resourceName + "Brush";
                if (Application.Current.Resources.Contains(brushName))
                {
                    Application.Current.Resources[brushName] = brush;
                }
            }
            catch { }
        }

        private void ButtonResetColors_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Сбросить все цвета к значениям по умолчанию?\n\n" +
                "Это перезапишет custom_colors.json",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Создаём новые дефолтные цвета
                var defaultColors = new CustomColors();

                // Обновляем TextBox
                TextBoxWindowBackgroundStart.Text = defaultColors.WindowBackgroundStart;
                TextBoxWindowBackgroundEnd.Text = defaultColors.WindowBackgroundEnd;
                TextBoxSelectedBackground.Text = defaultColors.SelectedBackground;
                TextBoxButtonSave.Text = defaultColors.ButtonSave;
                TextBoxButtonExclude.Text = defaultColors.ButtonExclude;
                TextBoxButtonInfo.Text = defaultColors.ButtonInfo;
                TextBoxTextPrimary.Text = defaultColors.TextPrimary;
                TextBoxTextSecondary.Text = defaultColors.TextSecondary;
                TextBoxRunningProcessText.Text = defaultColors.RunningProcessTextColor;

                // Обновляем предпросмотр
                UpdatePreview(TextBoxWindowBackgroundStart);
                UpdatePreview(TextBoxWindowBackgroundEnd);
                UpdatePreview(TextBoxSelectedBackground);
                UpdatePreview(TextBoxButtonSave);
                UpdatePreview(TextBoxButtonExclude);
                UpdatePreview(TextBoxButtonInfo);
                UpdatePreview(TextBoxTextPrimary);
                UpdatePreview(TextBoxTextSecondary);
                UpdatePreview(TextBoxRunningProcessText);

                // Применяем цвета
                CustomColorsManager.ApplyToResources(defaultColors);

                // Сохраняем в файл
                CustomColorsManager.Save(defaultColors);

                // 👈 Обновляем _currentColors И UpdatedColors
                _currentColors = defaultColors;
                UpdatedColors = defaultColors;

                MessageBox.Show("Цвета сброшены!", "Сброшено",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Excluded Apps

        private void LoadExcludedApps()
        {
            _excludedList = new ObservableCollection<string>();
            foreach (var excluded in _customData.ExcludedProcesses.OrderBy(x => x))
            {
                _excludedList.Add(excluded);
            }
            ListBoxExcluded.ItemsSource = _excludedList;
        }

        private void ListBoxExcluded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonRestore.IsEnabled = ListBoxExcluded.SelectedItem != null;
        }

        private void ButtonRestore_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxExcluded.SelectedItem is string excluded)
            {
                _customData.ExcludedProcesses.Remove(excluded);
                _excludedList.Remove(excluded);

                CustomDataManager.Save(_customData);

                if (_excludedList.Count == 0)
                    ButtonRestore.IsEnabled = false;
            }
        }

        private void ButtonClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_excludedList.Count == 0)
            {
                MessageBox.Show("Список исключений уже пуст.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Очистить весь список исключений ({_excludedList.Count} приложений)?\n\n" +
                $"Все приложения появятся в главном списке.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var excluded in _excludedList.ToList())
                {
                    _customData.ExcludedProcesses.Remove(excluded);
                }

                _excludedList.Clear();
                CustomDataManager.Save(_customData);
                ButtonRestore.IsEnabled = false;
            }
        }

        #endregion

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Helpers

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                        yield return t;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        #endregion
        #region General Settings

        private void LoadGeneralSettings()
        {
            // Загружаем текущий формат
            string currentFormat = _customData.TimeFormat ?? "hh_mm_ss";

            // Выбираем нужный пункт в ComboBox
            foreach (var item in ComboBoxTimeFormat.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    comboItem.Tag?.ToString() == currentFormat)
                {
                    ComboBoxTimeFormat.SelectedItem = item;
                    break;
                }
            }

            // Обновляем превью
            UpdateTimeFormatPreview();
        }

        private void ComboBoxTimeFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTimeFormatPreview();
        }

        private void UpdateTimeFormatPreview()
        {
            if (ComboBoxTimeFormat.SelectedItem is ComboBoxItem selectedItem)
            {
                string format = selectedItem.Tag?.ToString() ?? "hh_mm_ss";
                int sampleSeconds = 456789; // Пример: 127 часов

                string preview = FormatTime(sampleSeconds, format);
                TextBlockTimePreview.Text = preview;
            }
        }

        private string FormatTime(int totalSeconds, string format)
        {
            var time = TimeSpan.FromSeconds(totalSeconds);

            return format switch
            {
                "hours_int" => $"{(int)time.TotalHours} часов",
                "hours_float" => $"{time.TotalHours:F1} часов",
                "hh_mm" => $"{(int)time.TotalHours}:{time.Minutes:D2}",
                "hh_mm_ss" => $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}",
                "dd_hh_mm" => $"{(int)time.TotalDays}:{time.Hours:D2}:{time.Minutes:D2}",
                "dd_hh_mm_ss" => $"{(int)time.TotalDays}:{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}",
                _ => $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            };
        }

        private void ButtonApplySettings_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем формат времени
            if (ComboBoxTimeFormat.SelectedItem is ComboBoxItem selectedItem)
            {
                _customData.TimeFormat = selectedItem.Tag?.ToString() ?? "hh_mm_ss";
                CustomDataManager.Save(_customData);

                // Обновляем главное окно
                _mainWindow.RefreshTimeFormat();

                MessageBox.Show("Настройки применены!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Сбросить настройки к значениям по умолчанию?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _customData.TimeFormat = "hh_mm_ss";
                CustomDataManager.Save(_customData);

                LoadGeneralSettings();
                _mainWindow.RefreshTimeFormat();

                MessageBox.Show("Настройки сброшены!", "Сброшено",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}