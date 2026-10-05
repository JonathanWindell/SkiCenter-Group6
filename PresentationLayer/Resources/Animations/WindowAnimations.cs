using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PresentationLayer.Resources.Animations
{
    public static class WindowAnimations
    {
        public static async Task ExpandWindowAsync(FrameworkElement animatedBorder, double targetWidth, double targetHeight, int durationMs = 450)
        {
            // 1. Define easing parameters
            var duration = TimeSpan.FromMilliseconds(durationMs);
            var ease = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 6 };

            DoubleAnimation borderWidthAnim = new DoubleAnimation(animatedBorder.ActualWidth, targetWidth, duration) { EasingFunction = ease };
            DoubleAnimation borderHeightAnim = new DoubleAnimation(animatedBorder.ActualHeight, targetHeight, duration) { EasingFunction = ease };

            animatedBorder.BeginAnimation(FrameworkElement.WidthProperty, borderWidthAnim);
            animatedBorder.BeginAnimation(FrameworkElement.HeightProperty, borderHeightAnim);

            await Task.Delay(durationMs);
        }

        /// <summary>
        /// Fades out specific elements, resizes the main border, and smoothly swaps to a new window.
        /// </summary>
        public static async Task TransitionToWindowAsync(
            Window currentWindow,
            Window nextWindow,
            Border mainBorder,
            double targetWidth,
            double targetHeight,
            int durationMs = 200,
            params UIElement[] elementsToFadeOut)
        {
            // 1. Fade out the provided elements (if any)
            if (elementsToFadeOut != null && elementsToFadeOut.Length > 0)
            {
                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                foreach (var element in elementsToFadeOut)
                {
                    element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }

                await Task.Delay(durationMs);
            }

            // 2. Start animation (Using your existing ExpandWindowAsync method)
            await ExpandWindowAsync(mainBorder, targetWidth, targetHeight, durationMs * 2);

            // 3. Set manual startup location
            nextWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            // 4. Calculate exactly like your original working logout button
            double centerLeft = currentWindow.Left + (currentWindow.ActualWidth / 2);
            double centerTop = currentWindow.Top + (currentWindow.ActualHeight / 2);

            nextWindow.Left = centerLeft - (nextWindow.Width / 2);
            nextWindow.Top = centerTop - (nextWindow.Height / 2);

            // 5. Open next and close current
            nextWindow.Show();
            currentWindow.Close();
        }

        /// <summary>
        /// Smoothly fades in a Window from completely transparent to fully visible.
        /// </summary>
        public static async Task FadeInWindowAsync(Window window, int durationMs = 300)
        {
            // Ensure the window starts fully transparent before the animation kicks in
            window.Opacity = 0;

            var duration = TimeSpan.FromMilliseconds(durationMs);
            var fadeInAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            window.BeginAnimation(UIElement.OpacityProperty, fadeInAnim);

            // Wait for the animation to complete
            await Task.Delay(durationMs);
        }
    }
}