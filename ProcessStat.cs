using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppsTime.Models
{
    public class ProcessStat : INotifyPropertyChanged
    {
        private string _processName;
        private int _totalSeconds;

        public string ProcessName
        {
            get => _processName;
            set { _processName = value; OnPropertyChanged(); }
        }

        public int TotalSeconds
        {
            get => _totalSeconds;
            set { _totalSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeFormatted)); }
        }

        public string TimeFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(TotalSeconds);
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override string ToString() => $"{ProcessName} - {TimeFormatted}";
    }
}