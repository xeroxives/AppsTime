using System;
using System.Diagnostics;

namespace AppsTime.Helpers
{
    public static class AppLogger
    {
        public static bool DEBUG = false;

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            if (DEBUG)
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        [Conditional("DEBUG")]
        public static void LogError(string message)
        {
            if (DEBUG)
                Debug.WriteLine($"[❌ ERROR] {message}");
        }

        [Conditional("DEBUG")]
        public static void LogWarn(string message)
        {
            if (DEBUG)
                Debug.WriteLine($"[⚠️ WARN] {message}");
        }
    }
}