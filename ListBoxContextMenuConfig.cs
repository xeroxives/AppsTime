using System.Windows.Controls;

namespace AppsTime.Helpers
{
    /// <summary>
    /// Конфигурация контекстного меню ListBox
    /// </summary>
    public static class ListBoxContextMenuConfig
    {
        // 👇 Включить/выключить пункты меню
        public static bool ShowExclude { get; set; } = true;
        public static bool ShowCombine { get; set; } = true;
        public static bool ShowSetTag { get; set; } = true;
        public static bool ShowRename { get; set; } = true;
        public static bool ShowResetTime { get; set; } = true;
        public static bool ShowCopyName { get; set; } = true;
        public static bool ShowCopyTime { get; set; } = true;

        /// <summary>
        /// Применяет конфигурацию к ContextMenu
        /// </summary>
        public static void ApplyToMenu(ContextMenu menu)
        {
            if (menu == null) return;

            SetMenuItemVisible(menu, "MenuExclude", ShowExclude);
            SetMenuItemVisible(menu, "MenuCombine", ShowCombine);
            SetMenuItemVisible(menu, "MenuSetTag", ShowSetTag);
            SetMenuItemVisible(menu, "MenuRename", ShowRename);
            SetMenuItemVisible(menu, "MenuResetTime", ShowResetTime);
            SetMenuItemVisible(menu, "MenuCopyName", ShowCopyName);
            SetMenuItemVisible(menu, "MenuCopyTime", ShowCopyTime);
        }

        private static void SetMenuItemVisible(ContextMenu menu, string name, bool visible)
        {
            var item = menu.FindName(name) as MenuItem;
            if (item != null)
            {
                item.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }
    }
}