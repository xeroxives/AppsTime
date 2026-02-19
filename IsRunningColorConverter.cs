using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace AppsTime.Converters
{
    public class IsRunningColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = имя процесса (string)
            // values[1] = цвет для запущенных (Brush)
            // values[2] = цвет для остановленных (Brush)

            if (values.Length < 3)
                return Brushes.White;

            string processName = values[0] as string;
            Brush runningColor = values[1] as Brush;
            Brush stoppedColor = values[2] as Brush;

            if (string.IsNullOrWhiteSpace(processName))
                return stoppedColor ?? Brushes.White;

            // Проверяем, запущен ли процесс
            bool isRunning = Process.GetProcesses()
                .Any(p => string.Equals(p.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

            return isRunning ? (runningColor ?? Brushes.White) : (stoppedColor ?? Brushes.White);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}