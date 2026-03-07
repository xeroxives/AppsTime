using System.Windows;
using System.Windows.Controls;
using AppsTime.Controls;
using Application = System.Windows.Application;
using Panel = System.Windows.Controls.Panel;

namespace AppsTime.Helpers
{
	/// <summary>
	/// Статический менеджер для показа Toast-уведомлений
	/// Вызов из любого места: ToastManager.Success("Заголовок", "Текст")
	/// </summary>
	public static class ToastManager
	{
		// 👇 Ссылка на контейнер в MainWindow (устанавливается при инициализации)
		private static Panel _toastContainer;

		/// <summary>
		/// Инициализирует ToastManager (вызвать один раз при запуске приложения)
		/// </summary>
		/// <param name="container">Grid/Panel для размещения Toast (Panel.ZIndex=1000)</param>
		public static void Initialize(Panel container)
		{
			_toastContainer = container;
		}

		/// <summary>
		/// Показывает Toast с полной кастомизацией
		/// </summary>
		public static void Show(string title, string message, string icon = "ℹ️",
								string bgColor = "#FF2D2D30", string borderColor = "#FF505050",
								int durationSeconds = 5)
		{
			if (_toastContainer == null)
			{
				// Если контейнер не инициализирован — логируем и выходим
				AppLogger.Log("[Toast] Контейнер не инициализирован! Вызовите ToastManager.Initialize()", "ERROR");
				return;
			}

			// Если вызов не из UI-потока — маршалим в UI-поток
			if (!Application.Current.Dispatcher.CheckAccess())
			{
				Application.Current.Dispatcher.Invoke(() =>
					CreateAndShow(title, message, icon, bgColor, borderColor, durationSeconds));
				return;
			}

			CreateAndShow(title, message, icon, bgColor, borderColor, durationSeconds);
		}

		/// <summary>
		/// Внутренний метод создания и показа Toast
		/// </summary>
		private static void CreateAndShow(string title, string message, string icon,
										  string bgColor, string borderColor, int duration)
		{
			var toast = new ToastNotification();
			toast.Configure(title, message, icon, bgColor, borderColor, duration);

			_toastContainer.Children.Add(toast);
			toast.Show();
		}

		// ============================================
		// 👇 ПРЕДУСТАНОВЛЕННЫЕ СТИЛИ (рекомендую использовать их)
		// ============================================

		/// <summary>
		/// ℹ️ Информационное уведомление (синий)
		/// </summary>
		public static void Info(string title, string message, int duration = 5)
			=> Show(title, message, "ℹ️", "#EF1E3A5F", "#FF2E5A82", duration);

		/// <summary>
		/// ✅ Успешное уведомление (зелёный)
		/// </summary>
		public static void Success(string title, string message, int duration = 5)
			=> Show(title, message, "✔", "#EF1B5E20", "#FF2E7D32", duration);

		/// <summary>
		/// ⚠️ Предупреждение (оранжевый)
		/// </summary>
		public static void Warning(string title, string message, int duration = 7)
			=> Show(title, message, "⚠️", "#EFB54E00", "#FFF9A825", duration);

		/// <summary>
		/// ❌ Ошибка (красный)
		/// </summary>
		public static void Error(string title, string message, int duration = 7)
			=> Show(title, message, "❌", "#EFB71C1C", "#FFC62828", duration);
	}
}