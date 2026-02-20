using System;
using System.Collections.Generic;

namespace AppsTime.Models
{
    public class CustomData
    {
        public string Version { get; set; } = "1.0";
        public string TimeFormat { get; set; } = "hh_mm_ss";
        public DateTime LastModified { get; set; } = DateTime.Now;

        // 👇 Изменения: оригинальное имя → новое имя
        public Dictionary<string, string> NameAliases { get; set; } = new Dictionary<string, string>();

        // 👇 Изменения времени: оригинальное имя → новое время (в секундах)
        public Dictionary<string, int> TimeOverrides { get; set; } = new Dictionary<string, int>();

        // 👇 Список исключённых процессов
        public HashSet<string> ExcludedProcesses { get; set; } = new HashSet<string>();
    }
}