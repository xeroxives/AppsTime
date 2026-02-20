using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppsTime.Models
{
    public class ProcessStat : INotifyPropertyChanged
    {
        private string _processName;
        private int _totalSeconds;
        private string _timeFormat = "hh_mm_ss"; // По умолчанию

        // 👇 Статическое свойство для глобального формата
        public static string GlobalTimeFormat { get; set; } = "hh_mm_ss";

        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value;
                OnPropertyChanged();
            }
        }

        public int TotalSeconds
        {
            get => _totalSeconds;
            set
            {
                _totalSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeFormatted));
            }
        }

        public string OriginalKey { get; set; }

        // 👇 Форматирование с учётом глобальной настройки
        public string TimeFormatted
        {
            get
            {
                var time = TimeSpan.FromSeconds(TotalSeconds);
                string format = GlobalTimeFormat ?? "hh_mm_ss";

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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}