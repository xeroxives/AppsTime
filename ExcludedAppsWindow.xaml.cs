using AppsTime.Data;
using AppsTime.Helpers;
using AppsTime.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace AppsTime
{
    public partial class ExcludedAppsWindow : Window
    {
        private readonly CustomData _customData;
        public ObservableCollection<string> ExcludedList { get; } = new ObservableCollection<string>();

        public ExcludedAppsWindow(Window owner, CustomData customData)
        {
            InitializeComponent();
            Owner = owner;
            _customData = customData;
            LoadExcludedApps();
        }

        private void LoadExcludedApps()
        {
            ExcludedList.Clear();
            foreach (var excluded in _customData.ExcludedProcesses.OrderBy(x => x))
            {
                ExcludedList.Add(excluded);
            }
            ListBoxExcluded.ItemsSource = ExcludedList;

            AppLogger.Log($"[Excluded] Загружено {ExcludedList.Count} приложений");
        }

        private void ListBoxExcluded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonRestore.IsEnabled = ListBoxExcluded.SelectedItem != null;
        }

        private void ButtonRestore_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxExcluded.SelectedItem is string excluded)
            {
                var result = MessageBox.Show(
                    $"Восстановить \"{excluded}\" в список?\n\n" +
                    $"Приложение появится после обновления.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _customData.ExcludedProcesses.Remove(excluded);
                    ExcludedList.Remove(excluded);

                    CustomDataManager.Save(_customData);
                    AppLogger.Log($"[Excluded] Восстановлен: {excluded}");

                    if (ExcludedList.Count == 0)
                        ButtonRestore.IsEnabled = false;
                }
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}