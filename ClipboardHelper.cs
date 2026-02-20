using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AppsTime.Helpers
{
    public static class ClipboardHelper
    {
        /// <summary>
        /// Безопасная запись в буфер обмена с повторными попытками через Dispatcher
        /// </summary>
        public static async Task<bool> SetTextAsync(string text, int maxRetries = 5, int retryDelayMs = 50)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // 👇 Используем SetDataObject — иногда надёжнее
                    var dataObject = new DataObject();
                    dataObject.SetText(text);
                    Clipboard.SetDataObject(dataObject, true);

                    AppLogger.Log($"[Clipboard] Успешно записано: {text?.Substring(0, Math.Min(20, text?.Length ?? 0))}...");
                    return true;
                }
                catch (COMException ex) when (ex.Message.Contains("CLIPBRD_E_CANT_OPEN") || ex.ErrorCode == unchecked((int)0x800401D0))
                {
                    AppLogger.LogWarn($"[Clipboard] Попытка {i + 1}/{maxRetries} не удалась: буфер занят");

                    if (i < maxRetries - 1)
                    {
                        // 👇 Yield через Dispatcher вместо Thread.Sleep
                        await Task.Delay(retryDelayMs);
                        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"[Clipboard] Ошибка: {ex.GetType().Name}: {ex.Message}");
                    break;
                }
            }

            AppLogger.LogError("[Clipboard] Не удалось записать в буфер после всех попыток");
            return false;
        }

        /// <summary>
        /// Синхронная версия для простых случаев
        /// </summary>
        public static System.Threading.Tasks.Task<bool> SetText(string text, int maxRetries = 5, int retryDelayMs = 50)
        {
            // Запускаем асинхронную версию и ждем результат
            return Application.Current.Dispatcher.Invoke(async () =>
                await SetTextAsync(text, maxRetries, retryDelayMs));
        }

        public static async Task<string> GetTextAsync(int maxRetries = 5, int retryDelayMs = 50)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return Clipboard.GetText();
                }
                catch (COMException ex) when (ex.Message.Contains("CLIPBRD_E_CANT_OPEN"))
                {
                    AppLogger.LogWarn($"[Clipboard] Попытка {i + 1}/{maxRetries} не удалась: буфер занят");

                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelayMs);
                        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"[Clipboard] Ошибка: {ex.Message}");
                    break;
                }
            }

            return string.Empty;
        }
    }
}