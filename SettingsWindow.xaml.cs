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

            // Применяем локализацию при открытии
            ApplyLocalization();

            LoadLanguage();
            LoadColors();
            LoadExcludedApps();
            LoadGeneralSettings();
        }

        // 👇 Применяет локализацию ко всем элементам окна
        // 👇 Применяет локализацию ко всем элементам окна
        // 👇 Применяет локализацию ко всем элементам окна
        // 👇 Применяет локализацию ко всем элементам окна
        private void ApplyLocalization()
        {
            string lang = _customData.Language ?? "ru";

            // Заголовок окна
            Title = (lang == "en") ? "Settings" : "Настройки";

            // Заголовок "Настройки приложения"
            var labelTitle = FindName("LabelSettingsTitle") as Label;
            if (labelTitle != null)
                labelTitle.Content = (lang == "en") ? "Application Settings" : "Настройки приложения";

            // === Вкладки ===
            var tabGeneral = FindName("TabGeneral") as TabItem;
            if (tabGeneral != null)
                tabGeneral.Header = (lang == "en") ? "⚙️ General" : "⚙️ Общие";

            var tabColors = FindName("TabColors") as TabItem;
            if (tabColors != null)
                tabColors.Header = (lang == "en") ? "🎨 Colors" : "🎨 Цвета";

            var tabExcluded = FindName("TabExcluded") as TabItem;
            if (tabExcluded != null)
                tabExcluded.Header = (lang == "en") ? "📋 Excluded" : "📋 Исключённые";

            // === Вкладка Общие ===
            var groupLanguage = FindName("GroupBoxLanguage") as GroupBox;
            if (groupLanguage != null)
                groupLanguage.Header = (lang == "en") ? "Interface Language" : "Язык интерфейса";

            var labelLanguage = FindName("LabelLanguage") as TextBlock;
            if (labelLanguage != null)
                labelLanguage.Text = (lang == "en") ? "Language:" : "Язык:";

            var groupTimeFormat = FindName("GroupBoxTimeFormat") as GroupBox;
            if (groupTimeFormat != null)
                groupTimeFormat.Header = (lang == "en") ? "Time Format" : "Формат отображения времени";

            var labelTimeFormat = FindName("LabelTimeFormat") as TextBlock;
            if (labelTimeFormat != null)
                labelTimeFormat.Text = (lang == "en") ? "Time format:" : "Формат времени:";

            var labelExample = FindName("LabelExample") as TextBlock;
            if (labelExample != null)
                labelExample.Text = (lang == "en") ? "Example:" : "Пример:";

            // === Форматы времени (ComboBox items) ===
            UpdateComboBoxItem("int_timeformat", "124 часов (целые часы)", "124 hours (integer)");
            UpdateComboBoxItem("float_timeformat", "123.5 часов (дробные часы)", "123.5 hours (float)");
            UpdateComboBoxItem("h_m_timeformat", "123:30 (часы:минуты)", "123:30 (hours:minutes)");
            UpdateComboBoxItem("h_m_s_timeformat", "123:30:00 (часы:мин:сек)", "123:30:00 (hours:minutes:sec)");
            UpdateComboBoxItem("d_h_m_timeformat", "5:03:30 (дни:часы:мин)", "5:03:30 (day:hours:minutes)");
            UpdateComboBoxItem("d_h_m_s_timeformat", "5:03:30:00 (дни:часы:мин:сек)", "5:03:30:00 (dd:HH:MM:ss)");

            // Кнопки вкладки Общие
            if (ButtonResetSettings != null)
                ButtonResetSettings.Content = (lang == "en") ? "🔄 Reset" : "🔄 Сбросить";

            if (ButtonApplySettings != null)
                ButtonApplySettings.Content = (lang == "en") ? "💾 Apply" : "💾 Применить";

            // === Вкладка Цвета ===
            var groupMainColors = FindName("GroupBoxMainColors") as GroupBox;
            if (groupMainColors != null)
                groupMainColors.Header = (lang == "en") ? "Main colors" : "Основные цвета";

            var labelBackgroundStart = FindName("LabelBackgroundStart") as TextBlock;
            if (labelBackgroundStart != null)
                labelBackgroundStart.Text = (lang == "en") ? "Background (start):" : "Фон (начало):";

            var labelBackgroundEnd = FindName("LabelBackgroundEnd") as TextBlock;
            if (labelBackgroundEnd != null)
                labelBackgroundEnd.Text = (lang == "en") ? "Background (end):" : "Фон (конец):";

            var labelSelection = FindName("LabelSelection") as TextBlock;
            if (labelSelection != null)
                labelSelection.Text = (lang == "en") ? "Selection:" : "Выделение:";

            var groupButtonColors = FindName("GroupBoxButtonColors") as GroupBox;
            if (groupButtonColors != null)
                groupButtonColors.Header = (lang == "en") ? "Button colors" : "Цвета кнопок";

            var groupTextColors = FindName("GroupBoxTextColors") as GroupBox;
            if (groupTextColors != null)
                groupTextColors.Header = (lang == "en") ? "Text colors" : "Цвета текста";

            var labelPrimary = FindName("LabelPrimary") as TextBlock;
            if (labelPrimary != null)
                labelPrimary.Text = (lang == "en") ? "Primary:" : "Основной:";

            var labelSecondary = FindName("LabelSecondary") as TextBlock;
            if (labelSecondary != null)
                labelSecondary.Text = (lang == "en") ? "Secondary:" : "Вторичный:";

            var labelRunning = FindName("LabelRunning") as TextBlock;
            if (labelRunning != null)
                labelRunning.Text = (lang == "en") ? "Running:" : "Запущенные:";

            // Кнопки вкладки Цвета
            if (ButtonResetColors != null)
                ButtonResetColors.Content = (lang == "en") ? "🔄 Reset" : "🔄 Сбросить";

            if (ButtonApplyColors != null)
                ButtonApplyColors.Content = (lang == "en") ? "💾 Apply" : "💾 Применить";

            // === Вкладка Исключённые ===
            if (ButtonRestore != null)
                ButtonRestore.Content = (lang == "en") ? "Restore" : "Восстановить";

            if (ButtonClearAll != null)
                ButtonClearAll.Content = (lang == "en") ? "Clear all" : "Очистить все";

            // Кнопка Закрыть
            if (ButtonClose != null)
                ButtonClose.Content = (lang == "en") ? "Close" : "Закрыть";
        }

        // 👇 Обновляет текст элемента ComboBox
        private void UpdateComboBoxItem(string itemName, string ruText, string enText)
        {
            var item = FindName(itemName) as ComboBoxItem;
            if (item != null)
                item.Content = (_customData.Language ?? "ru") == "en" ? enText : ruText;
        }

        // 👇 Helper для получения текста на нужном языке
        private string GetText(string ru, string en)
        {
            return (_customData.Language ?? "ru") == "en" ? en : ru;
        }

        // 👇 Обновляет текст метки
        //private void UpdateLabelText(string ruText, string enText)
        //{
        //    var label = FindVisualChildren<TextBlock>((Grid)Content)
        //        .FirstOrDefault(t => t.Text == ruText || t.Text == enText);
        //    if (label != null)
        //        label.Text = (_customData.Language ?? "ru") == "en" ? enText : ruText;
        //}

        // 👇 Helper для получения текста на нужном языке

        #region General Settings

        private void LoadGeneralSettings()
        {
            // Загружаем язык
            LoadLanguage();

            // Загружаем формат времени
            string currentFormat = _customData.TimeFormat ?? "hh_mm_ss";

            foreach (var item in ComboBoxTimeFormat.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    comboItem.Tag?.ToString() == currentFormat)
                {
                    ComboBoxTimeFormat.SelectedItem = item;
                    break;
                }
            }

            UpdateTimeFormatPreview();
        }

        private void LoadLanguage()
        {
            string currentLang = _customData.Language ?? "ru";

            foreach (var item in ComboBoxLanguage.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    comboItem.Tag?.ToString() == currentLang)
                {
                    ComboBoxLanguage.SelectedItem = item;
                    break;
                }
            }
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
                int sampleSeconds = 456789;

                string preview = FormatTime(sampleSeconds, format);
                TextBlockTimePreview.Text = preview;
            }
        }

        private string FormatTime(int totalSeconds, string format)
        {
            var time = TimeSpan.FromSeconds(totalSeconds);

            return format switch
            {
                "hours_int" => $"{(int)time.TotalHours} " + GetText("часов", "hours"),
                "hours_float" => $"{time.TotalHours:F1} " + GetText("часов", "hours"),
                "hh_mm" => $"{(int)time.TotalHours}:{time.Minutes:D2}",
                "hh_mm_ss" => $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}",
                "dd_hh_mm" => $"{(int)time.TotalDays}:{time.Hours:D2}:{time.Minutes:D2}",
                "dd_hh_mm_ss" => $"{(int)time.TotalDays}:{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}",
                _ => $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            };
        }

        private void ButtonApplySettings_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем язык
            if (ComboBoxLanguage.SelectedItem is ComboBoxItem langItem)
            {
                string newLanguage = langItem.Tag?.ToString() ?? "ru";
                _customData.Language = newLanguage;
            }

            // Сохраняем формат времени
            if (ComboBoxTimeFormat.SelectedItem is ComboBoxItem timeItem)
            {
                _customData.TimeFormat = timeItem.Tag?.ToString() ?? "hh_mm_ss";
            }

            CustomDataManager.Save(_customData);

            // 👇 Обновляем локализацию в этом окне и главном
            ApplyLocalization();
            _mainWindow.ApplyLocalization();
            _mainWindow.RefreshTimeFormat();

            MessageBox.Show(
                GetText("Настройки применены!", "Settings applied!"),
                GetText("Успешно", "Success"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                GetText("Сбросить настройки?", "Reset settings?"),
                GetText("Подтверждение", "Confirm"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _customData.Language = "ru";
                _customData.TimeFormat = "hh_mm_ss";
                CustomDataManager.Save(_customData);

                LoadLanguage();
                LoadGeneralSettings();
                ApplyLocalization();
                _mainWindow.ApplyLocalization();
                _mainWindow.RefreshTimeFormat();

                MessageBox.Show(
                    GetText("Настройки сброшены!", "Settings reset!"),
                    GetText("Сброшено", "Reset"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Colors

        private void LoadColors()
        {
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

            TextBoxButtonSave.Text = _currentColors.ButtonSave;
            BorderButtonSave.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonSave));

            TextBoxButtonExclude.Text = _currentColors.ButtonExclude;
            BorderButtonExclude.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonExclude));

            TextBoxButtonInfo.Text = _currentColors.ButtonInfo;
            BorderButtonInfo.Background = AppColors.ToBrush(
                AppColors.ColorFromHex(_currentColors.ButtonInfo));

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

                    string borderName = textBox.Name.Replace("TextBox", "Border");
                    var border = FindName(borderName) as Border;
                    if (border != null)
                    {
                        border.Background = brush;
                    }
                }
            }
            catch { }
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

                _currentColors.WindowBackgroundStart = TextBoxWindowBackgroundStart.Text;
                _currentColors.WindowBackgroundEnd = TextBoxWindowBackgroundEnd.Text;
                _currentColors.SelectedBackground = TextBoxSelectedBackground.Text;
                _currentColors.ButtonSave = TextBoxButtonSave.Text;
                _currentColors.ButtonExclude = TextBoxButtonExclude.Text;
                _currentColors.ButtonInfo = TextBoxButtonInfo.Text;
                _currentColors.TextPrimary = TextBoxTextPrimary.Text;
                _currentColors.TextSecondary = TextBoxTextSecondary.Text;
                _currentColors.RunningProcessTextColor = TextBoxRunningProcessText.Text;

                UpdatedColors = _currentColors;

                if (CustomColorsManager.Save(_currentColors))
                {
                    MessageBox.Show(
                        GetText("Цвета применены и сохранены!", "Colors applied and saved!"),
                        GetText("Успешно", "Success"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{GetText("Ошибка", "Error")}: {ex.Message}",
                    GetText("Ошибка", "Error"),
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
                GetText("Сбросить все цвета к значениям по умолчанию?\n\nЭто перезапишет custom_colors.json",
                        "Reset all colors to default?\n\nThis will overwrite custom_colors.json"),
                GetText("Подтверждение", "Confirm"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var defaultColors = new CustomColors();

                TextBoxWindowBackgroundStart.Text = defaultColors.WindowBackgroundStart;
                TextBoxWindowBackgroundEnd.Text = defaultColors.WindowBackgroundEnd;
                TextBoxSelectedBackground.Text = defaultColors.SelectedBackground;
                TextBoxButtonSave.Text = defaultColors.ButtonSave;
                TextBoxButtonExclude.Text = defaultColors.ButtonExclude;
                TextBoxButtonInfo.Text = defaultColors.ButtonInfo;
                TextBoxTextPrimary.Text = defaultColors.TextPrimary;
                TextBoxTextSecondary.Text = defaultColors.TextSecondary;
                TextBoxRunningProcessText.Text = defaultColors.RunningProcessTextColor;

                UpdatePreview(TextBoxWindowBackgroundStart);
                UpdatePreview(TextBoxWindowBackgroundEnd);
                UpdatePreview(TextBoxSelectedBackground);
                UpdatePreview(TextBoxButtonSave);
                UpdatePreview(TextBoxButtonExclude);
                UpdatePreview(TextBoxButtonInfo);
                UpdatePreview(TextBoxTextPrimary);
                UpdatePreview(TextBoxTextSecondary);
                UpdatePreview(TextBoxRunningProcessText);

                CustomColorsManager.ApplyToResources(defaultColors);
                CustomColorsManager.Save(defaultColors);

                _currentColors = defaultColors;
                UpdatedColors = defaultColors;

                MessageBox.Show(
                    GetText("Цвета сброшены!", "Colors reset!"),
                    GetText("Сброшено", "Reset"),
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
                MessageBox.Show(
                    GetText("Список исключений уже пуст.", "Exclusion list is already empty."),
                    GetText("Информация", "Information"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"{GetText("Очистить весь список исключений", "Clear entire exclusion list")} ({_excludedList.Count} {GetText("приложений", "apps")})?\n\n" +
                $"{GetText("Все приложения появятся в главном списке.", "All apps will appear in the main list.")}",
                GetText("Подтверждение", "Confirm"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

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
    }
}