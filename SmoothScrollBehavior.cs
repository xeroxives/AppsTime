using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AppsTime.Behaviors
{
    public static class SmoothScrollBehavior
    {
        public static readonly DependencyProperty EnableSmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableSmoothScroll",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnEnableSmoothScrollChanged));

        public static void SetEnableSmoothScroll(UIElement element, bool value) =>
            element.SetValue(EnableSmoothScrollProperty, value);

        public static bool GetEnableSmoothScroll(UIElement element) =>
            (bool)element.GetValue(EnableSmoothScrollProperty);

        private static readonly DependencyProperty AnimatorProperty =
            DependencyProperty.RegisterAttached(
                "Animator",
                typeof(ScrollAnimator),
                typeof(SmoothScrollBehavior));

        private static void OnEnableSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                    listBox.PreviewMouseWheel += ListBox_PreviewMouseWheel;
                else
                    listBox.PreviewMouseWheel -= ListBox_PreviewMouseWheel;
            }
        }

        private static void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var scrollViewer = GetScrollViewer(listBox);
            if (scrollViewer == null) return;

            e.Handled = true;

            var animator = GetAnimator(scrollViewer);
            if (animator == null)
            {
                animator = new ScrollAnimator(scrollViewer);
                SetAnimator(scrollViewer, animator);
            }

            // 👇 Определяем направление
            double direction = e.Delta > 0 ? -1 : 1;

            // 👇 Передаём направление в аниматор (для акселерации)
            animator.ScrollWithAcceleration(direction);
        }

        private static ScrollViewer GetScrollViewer(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is ScrollViewer viewer)
                    return viewer;

                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static ScrollAnimator GetAnimator(DependencyObject obj) =>
            (ScrollAnimator)obj.GetValue(AnimatorProperty);

        private static void SetAnimator(DependencyObject obj, ScrollAnimator value) =>
            obj.SetValue(AnimatorProperty, value);

        private class ScrollAnimator
        {
            private readonly ScrollViewer _scrollViewer;
            private readonly DispatcherTimer _timer;
            private double _from;
            private double _to;
            private double _startTime;
            private const double BaseDuration = 250; // мс (базовая длительность)

            // 👇 Параметры акселерации
            private double _currentSpeed = 1.0; // Текущий множитель скорости
            private DateTime _lastScrollTime = DateTime.MinValue;
            private const double AccelerationFactor = 1.3; // Во сколько раз увеличивать скорость
            private const double MaxSpeed = 3.0; // Максимальный множитель
            private const double ResetTimeoutMs = 300; // Через сколько мс сбрасывать скорость
            private const double BaseScrollAmount = 10; // Базовый шаг прокрутки

            public ScrollAnimator(ScrollViewer scrollViewer)
            {
                _scrollViewer = scrollViewer;
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
                };
                _timer.Tick += Timer_Tick;
            }

            public void ScrollWithAcceleration(double direction)
            {
                var now = DateTime.Now;
                var timeSinceLastScroll = (now - _lastScrollTime).TotalMilliseconds;

                // 👇 Проверяем, продолжаем ли прокрутку в том же направлении
                bool isContinuation = timeSinceLastScroll < ResetTimeoutMs;

                if (isContinuation)
                {
                    // 👇 Увеличиваем скорость (акселерация)
                    _currentSpeed = Math.Min(_currentSpeed * AccelerationFactor, MaxSpeed);
                }
                else
                {
                    // 👇 Сбрасываем скорость (первый щелчок или смена направления)
                    _currentSpeed = 1.0;
                }

                _lastScrollTime = now;

                // 👇 Рассчитываем шаг с учётом текущей скорости
                double scrollAmount = BaseScrollAmount * _currentSpeed * direction;

                _from = _scrollViewer.VerticalOffset;
                _to = Math.Max(0, Math.Min(_from + scrollAmount, _scrollViewer.ScrollableHeight));
                _startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                // 👇 Чем выше скорость, тем короче анимация
                double duration = BaseDuration / Math.Sqrt(_currentSpeed);

                _timer.Stop();
                _timer.Start();
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                var elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _startTime;

                // 👇 Динамическая длительность на основе скорости
                double duration = BaseDuration / Math.Sqrt(_currentSpeed);
                double progress = Math.Min(1.0, elapsed / duration);

                // 👇 Easing функция (EaseOut Cubic) - плавное замедление в конце
                double easedProgress = 1 - Math.Pow(1 - progress, 3);

                var currentOffset = _from + (_to - _from) * easedProgress;
                _scrollViewer.ScrollToVerticalOffset(currentOffset);

                if (progress >= 1.0)
                    _timer.Stop();
            }
        }
    }
}