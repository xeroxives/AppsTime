using AppsTime.Helpers;
using AppsTime.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace AppsTime.Data
{
    public static class CustomColorsManager
    {
        private static readonly string FileName = "custom_colors.json";
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Загружает пользовательские цвета из файла
        /// </summary>
        public static CustomColors Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    AppLogger.Log("[CustomColors] Файл не найден, используем цвета по умолчанию");
                    return new CustomColors();
                }

                var json = File.ReadAllText(FilePath);
                var colors = JsonSerializer.Deserialize<CustomColors>(json, JsonOptions);

                AppLogger.Log($"[CustomColors] Загружено из {FilePath}");
                return colors ?? new CustomColors();
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[CustomColors] Ошибка загрузки: {ex.Message}");
                return new CustomColors();
            }
        }

        /// <summary>
        /// Сохраняет пользовательские цвета в файл
        /// </summary>
        public static bool Save(CustomColors colors)
        {
            try
            {
                var json = JsonSerializer.Serialize(colors, JsonOptions);
                File.WriteAllText(FilePath, json);

                AppLogger.Log($"[CustomColors] Сохранено в {FilePath}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[CustomColors] Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Применяет цвета к ресурсам приложения
        /// </summary>
        public static void ApplyToResources(CustomColors colors)
        {
            if (colors == null) return;

            SetColor("WindowBackgroundStart", colors.WindowBackgroundStart);
            SetColor("WindowBackgroundEnd", colors.WindowBackgroundEnd);
            SetColor("SelectedBackground", colors.SelectedBackground);
            SetColor("HoverBackground", colors.HoverBackground);

            SetColor("TextPrimary", colors.TextPrimary);
            SetColor("TextSecondary", colors.TextSecondary);
            SetColor("ButtonText", colors.ButtonText);
            SetColor("RunningProcessTextColor", colors.RunningProcessTextColor);

            SetColor("ButtonSave", colors.ButtonSave);
            SetColor("ButtonSaveHover", colors.ButtonSaveHover);
            SetColor("ButtonSavePressed", colors.ButtonSavePressed);

            SetColor("ButtonExclude", colors.ButtonExclude);
            SetColor("ButtonExcludeHover", colors.ButtonExcludeHover);
            SetColor("ButtonExcludePressed", colors.ButtonExcludePressed);

            SetColor("ButtonInfo", colors.ButtonInfo);
            SetColor("ButtonInfoHover", colors.ButtonInfoHover);
            SetColor("ButtonInfoPressed", colors.ButtonInfoPressed);

            SetColor("ButtonDefault", colors.ButtonDefault);
            SetColor("ButtonDefaultHover", colors.ButtonDefaultHover);
            SetColor("ButtonDefaultPressed", colors.ButtonDefaultPressed);

            SetColor("TextBoxBackground", colors.TextBoxBackground);
            SetColor("TextBoxText", colors.TextBoxText);
            SetColor("TextBoxBorder", colors.TextBoxBorder);

            SetColor("ListBoxBackground", colors.ListBoxBackground);
            SetColor("ListBoxText", colors.ListBoxText);
            SetColor("ListBoxBorder", colors.ListBoxBorder);
        }

        private static void SetColor(string resourceName, string hexColor)
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
            catch (Exception ex)
            {
                AppLogger.LogError($"[CustomColors] Ошибка применения {resourceName}: {ex.Message}");
            }
        }
    }
}