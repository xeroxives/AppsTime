using System.Windows.Media;

namespace AppsTime.Helpers
{
    public static class AppColors
    {
        // ─────────────────────────────────────────────────────
        // Основные цвета интерфейса
        // ─────────────────────────────────────────────────────

        /// <summary>Фон окна (градиент начало)</summary>
        public static Color WindowBackgroundStart => ColorFromHex("#FF565656");

        /// <summary>Фон окна (градиент конец)</summary>
        public static Color WindowBackgroundEnd => ColorFromHex("#FF353535");

        /// <summary>Фон панелей и контейнеров</summary>
        public static Color PanelBackground => ColorFromHex("#FF404040");

        /// <summary>Фон выделенного элемента в списке</summary>
        public static Color SelectedBackground => ColorFromHex("#FF505050");

        /// <summary>Фон при наведении на элемент</summary>
        public static Color HoverBackground => ColorFromHex("#FF484848");

        // ─────────────────────────────────────────────────────
        // Цвета текста
        // ─────────────────────────────────────────────────────

        /// <summary>Основной цвет текста</summary>
        public static Color TextPrimary => ColorFromHex("#FFFFFFFF");

        /// <summary>Вторичный цвет текста (метки, подсказки)</summary>
        public static Color TextSecondary => ColorFromHex("#FF888888");

        /// <summary>Цвет текста на кнопках</summary>
        public static Color ButtonText => ColorFromHex("#FFFFFFFF");

        // ─────────────────────────────────────────────────────
        // Цвета кнопок (основные)
        // ─────────────────────────────────────────────────────

        /// <summary>Кнопка "Save" (зелёная)</summary>
        public static Color ButtonSave => ColorFromHex("#FF27AE60");
        public static Color ButtonSaveHover => ColorFromHex("#FF2AB365");
        public static Color ButtonSavePressed => ColorFromHex("#FF249555");

        /// <summary>Кнопка "Exclude" (красная)</summary>
        public static Color ButtonExclude => ColorFromHex("#FFC0392B");
        public static Color ButtonExcludeHover => ColorFromHex("#FFC94435");
        public static Color ButtonExcludePressed => ColorFromHex("#FFA93226");

        /// <summary>Кнопка "Excluded Apps" (синяя)</summary>
        public static Color ButtonInfo => ColorFromHex("#FF288CC8");
        public static Color ButtonInfoHover => ColorFromHex("#FF3095D0");
        public static Color ButtonInfoPressed => ColorFromHex("#FF227AB0");

        /// <summary>Кнопка "Закрыть" (серая)</summary>
        public static Color ButtonDefault => ColorFromHex("#FF505050");
        public static Color ButtonDefaultHover => ColorFromHex("#FF585858");
        public static Color ButtonDefaultPressed => ColorFromHex("#FF484848");

        /// <summary>Отключенная кнопка</summary>
        public static Color ButtonDisabled => ColorFromHex("#FF505050");
        public static Color ButtonDisabledText => ColorFromHex("#FF888888");

        // ─────────────────────────────────────────────────────
        // Цвета элементов управления
        // ─────────────────────────────────────────────────────

        /// <summary>Фон TextBox</summary>
        public static Color TextBoxBackground => ColorFromHex("#FF404040");

        /// <summary>Текст в TextBox</summary>
        public static Color TextBoxText => ColorFromHex("#FFFFFFFF");

        /// <summary>Рамка TextBox</summary>
        public static Color TextBoxBorder => ColorFromHex("#FF606060");

        /// <summary>Фон ListBox</summary>
        public static Color ListBoxBackground => ColorFromHex("#FF404040");

        /// <summary>Текст в ListBox</summary>
        public static Color ListBoxText => ColorFromHex("#FFFFFFFF");

        /// <summary>Рамка ListBox</summary>
        public static Color ListBoxBorder => ColorFromHex("#FF606060");

        // ─────────────────────────────────────────────────────
        // Утилиты
        // ─────────────────────────────────────────────────────

        /// <summary>Конвертирует HEX строку в Color</summary>
        public static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        /// <summary>Создаёт SolidColorBrush из цвета</summary>
        public static SolidColorBrush ToBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        /// <summary>Создаёт SolidColorBrush из HEX</summary>
        public static SolidColorBrush BrushFromHex(string hex)
        {
            return new SolidColorBrush(ColorFromHex(hex));
        }
    }
}