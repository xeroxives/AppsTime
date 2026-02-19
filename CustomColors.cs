using System.Windows.Media;

namespace AppsTime.Models
{
    public class CustomColors
    {
        public string Version { get; set; } = "1.0";

        // Основные цвета
        public string WindowBackgroundStart { get; set; } = "#FF565656";
        public string WindowBackgroundEnd { get; set; } = "#FF353535";
        public string SelectedBackground { get; set; } = "#FF505050";
        public string HoverBackground { get; set; } = "#FF484848";

        // Текст
        public string TextPrimary { get; set; } = "#FFFFFFFF";
        public string TextSecondary { get; set; } = "#FF888888";
        public string ButtonText { get; set; } = "#FFFFFFFF";

        // Кнопки
        public string ButtonSave { get; set; } = "#FF27AE60";
        public string ButtonSaveHover { get; set; } = "#FF2AB365";
        public string ButtonSavePressed { get; set; } = "#FF249555";

        public string ButtonExclude { get; set; } = "#FFC0392B";
        public string ButtonExcludeHover { get; set; } = "#FFC94435";
        public string ButtonExcludePressed { get; set; } = "#FFA93226";

        public string ButtonInfo { get; set; } = "#FF288CC8";
        public string ButtonInfoHover { get; set; } = "#FF3095D0";
        public string ButtonInfoPressed { get; set; } = "#FF227AB0";

        public string ButtonDefault { get; set; } = "#FF505050";
        public string ButtonDefaultHover { get; set; } = "#FF585858";
        public string ButtonDefaultPressed { get; set; } = "#FF484848";

        // Элементы управления
        public string TextBoxBackground { get; set; } = "#FF404040";
        public string TextBoxText { get; set; } = "#FFFFFFFF";
        public string TextBoxBorder { get; set; } = "#FF606060";

        public string ListBoxBackground { get; set; } = "#FF404040";
        public string ListBoxText { get; set; } = "#FFFFFFFF";
        public string ListBoxBorder { get; set; } = "#FF606060";
        public string RunningProcessTextColor { get; set; } = "#FF40C575";
    }
}