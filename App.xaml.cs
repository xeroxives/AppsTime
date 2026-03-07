using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Application = System.Windows.Application;

namespace AppsTime
{
	public partial class App : Application
	{
		private const string MutexName = "AppsTime_SingleInstance_Mutex";
		private const string EventName = "AppsTime_Restore_Event";
		private static Mutex _mutex;
		private static EventWaitHandle _restoreEvent;
		private static bool _ownsMutex = false;  // 👇 Флаг: владеет ли приложение мьютексом

		protected override void OnStartup(StartupEventArgs e)
		{
			bool createdNew;
			_mutex = new Mutex(true, MutexName, out createdNew);

			// 👇 Запоминаем, владеет ли это приложение мьютексом
			_ownsMutex = createdNew;

			if (!createdNew)
			{
				// Приложение уже запущено - отправляем сигнал на восстановление
				SignalRestore();
				Current.Shutdown();
				return;
			}

			base.OnStartup(e);

			var mainWindow = new MainWindow();
			mainWindow.Show();

			// Запускаем прослушивание сигнала восстановления
			StartRestoreListener();
		}

		private static void SignalRestore()
		{
			try
			{
				using (var evt = EventWaitHandle.OpenExisting(EventName))
				{
					evt.Set();
				}
				Thread.Sleep(100);
			}
			catch
			{
				// Игнорируем
			}
		}

		private static void StartRestoreListener()
		{
			_restoreEvent = new EventWaitHandle(
				false,
				EventResetMode.AutoReset,
				EventName);

			var thread = new Thread(() =>
			{
				while (true)
				{
					if (_restoreEvent.WaitOne())
					{
						Application.Current.Dispatcher.Invoke(() =>
						{
							RestoreMainWindow();
						});
					}
				}
			})
			{
				IsBackground = true
			};
			thread.Start();
		}

		private static void RestoreMainWindow()
		{
			var mainWindow = Application.Current.MainWindow as MainWindow;
			if (mainWindow != null)
			{
				mainWindow.RestoreFromTray();
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			// 👇 Останавливаем слушатель
			_restoreEvent?.Set();  // Разблокируем WaitOne() чтобы поток завершился
			_restoreEvent?.Dispose();

			// 👇 Освобождаем мьютекс ТОЛЬКО если мы им владеем
			if (_ownsMutex && _mutex != null)
			{
				try
				{
					_mutex.ReleaseMutex();
				}
				catch (ApplicationException)
				{
					// Мьютекс уже освобождён или не принадлежит нам - игнорируем
				}
				finally
				{
					_mutex.Dispose();
					_mutex = null;
				}
			}

			base.OnExit(e);
		}
	}
}