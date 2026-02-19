using System;

namespace AppsTime.Models
{
    public class AppUsageEntry
    {
        public string ProcessName { get; set; }
        public int TimeSeconds { get; set; }
        public string Description { get; set; }
        public DateTime LogDate { get; set; }

        // Удобное свойство для отображения времени
        public string TimeFormatted => TimeSpan.FromSeconds(TimeSeconds).ToString(@"hh\:mm\:ss");

        public override string ToString() =>
            $"{ProcessName} — {TimeFormatted}{(string.IsNullOrWhiteSpace(Description) ? "" : $" ({Description})")}";
    }
}