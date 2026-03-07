using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Panel = System.Windows.Controls.Panel;
using UserControl = System.Windows.Controls.UserControl;

namespace AppsTime.Controls
{
	public partial class ToastNotification : UserControl
	{
		private DispatcherTimer _hideTimer;

		public ToastNotification()
		{
			InitializeComponent();

			_hideTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(5),
				IsEnabled = false
			};
			_hideTimer.Tick += (s, e) => Hide();
		}

		/// <summary>
		/// Настраивает Toast перед показом
		/// </summary>
		public void Configure(string title, string message, string icon = "ℹ️",
							  string bgColor = "#FF2D2D30", string borderColor = "#FF505050",
							  int durationSeconds = 5)
		{
			ToastTitle.Text = title;
			ToastMessage.Text = message;
			ToastIcon.Text = icon;

			try
			{
				ToastBorder.Background = new SolidColorBrush(
					(Color)ColorConverter.ConvertFromString(bgColor));
				ToastBorder.BorderBrush = new SolidColorBrush(
					(Color)ColorConverter.ConvertFromString(borderColor));
			}
			catch
			{
				// Если цвет невалидный — используем дефолтный
			}

			_hideTimer.Interval = TimeSpan.FromSeconds(durationSeconds);
		}

		/// <summary>
		/// Показывает Toast с анимацией
		/// </summary>
		public void Show()
		{
			// Анимация появления (fade in)
			var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
			{
				From = 0,
				To = 1,
				Duration = TimeSpan.FromMilliseconds(250),
				EasingFunction = new System.Windows.Media.Animation.QuadraticEase
				{
					EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
				}
			};
			this.BeginAnimation(OpacityProperty, fadeIn);

			// Запускаем таймер авто-скрытия
			_hideTimer.Start();
		}

		/// <summary>
		/// Скрывает Toast с анимацией и удаляет из контейнера
		/// </summary>
		private void Hide()
		{
			_hideTimer.Stop();

			// Анимация исчезновения (fade out)
			var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
			{
				From = Opacity,
				To = 0,
				Duration = TimeSpan.FromMilliseconds(250),
				EasingFunction = new System.Windows.Media.Animation.QuadraticEase
				{
					EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
				}
			};

			fadeOut.Completed += (s, e) =>
			{
				// Удаляем из родительского контейнера
				if (this.Parent is Panel parent)
				{
					parent.Children.Remove(this);
				}
			};

			this.BeginAnimation(OpacityProperty, fadeOut);
		}

		private void ButtonClose_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}
	}
}