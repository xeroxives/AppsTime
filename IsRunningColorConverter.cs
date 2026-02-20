using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using AppsTime.Data;

namespace AppsTime.Converters
{
    public class IsRunningColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = отображаемое имя процесса (string)
            // values[1] = цвет для запущенных (Brush)
            // values[2] = цвет для остановленных (Brush)

            if (values.Length < 3)
                return Brushes.White;

            string displayName = values[0] as string;
            Brush runningColor = values[1] as Brush;
            Brush stoppedColor = values[2] as Brush;

            if (string.IsNullOrWhiteSpace(displayName))
                return stoppedColor ?? Brushes.White;

            // 👇 Получаем оригинальное имя из алиасов (если есть)
            string originalName = displayName;
            var customData = CustomDataManager.Load();

            foreach (var alias in customData.NameAliases)
            {
                // Если алиас ведёт к отображаемому имени
                if (alias.Value == displayName)
                {
                    originalName = alias.Key;
                    break;
                }
            }

            // 👇 Проверяем оба имени: и оригинальное, и отображаемое
            bool isRunning = Process.GetProcesses()
                .Any(p =>
                    string.Equals(p.ProcessName, originalName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.ProcessName, displayName, StringComparison.OrdinalIgnoreCase));

            return isRunning ? (runningColor ?? Brushes.White) : (stoppedColor ?? Brushes.White);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}