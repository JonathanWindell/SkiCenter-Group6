using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PresentationLayer.Resources.Animations
{
    public static class ElementAnimations
    {
        public static async Task ToggleLoadingAnimationAsync(bool isLoading, UIElement fadeOutPanel, UIElement loadingVisual, RotateTransform spinnerRotate)
        {
            var duration = TimeSpan.FromMilliseconds(400);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            if (isLoading)
            {
                // 1. Block user input to prevent multiple clicks
                fadeOutPanel.IsHitTestVisible = false;

                DoubleAnimation fadeForm = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
                fadeOutPanel.BeginAnimation(UIElement.OpacityProperty, fadeForm);

                DoubleAnimation fadeSpinner = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
                loadingVisual.BeginAnimation(UIElement.OpacityProperty, fadeSpinner);

                StartSpinner(spinnerRotate);

                // Wait for the fade-in to complete
                await Task.Delay(duration);
            }
            else
            {
                DoubleAnimation fadeSpinner = new DoubleAnimation(1, 0, duration) { EasingFunction = ease };
                loadingVisual.BeginAnimation(UIElement.OpacityProperty, fadeSpinner);

                DoubleAnimation fadeForm = new DoubleAnimation(0, 1, duration) { EasingFunction = ease };
                fadeOutPanel.BeginAnimation(UIElement.OpacityProperty, fadeForm);

                // 2. WAIT for the fade-out animation to actually finish 
                await Task.Delay(duration);

                // 3. Stop the spinning animation and allow user input again 
                spinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
                fadeOutPanel.IsHitTestVisible = true;
            }
        }

        public static async Task AnimateMainMenuAsync(UIElement headerBar, TranslateTransform headerTranslate, UIElement menuContentStack, TranslateTransform contentTranslate)
        {
            var duration = TimeSpan.FromMilliseconds(600);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            // --- 1. Animate header
            headerBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
            headerTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-10, 0, duration) { EasingFunction = ease });

            await Task.Delay(150);

            // 2. Animate content (The menu options)
            menuContentStack.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
            contentTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(20, 0, duration) { EasingFunction = ease });
        }

        private static void StartSpinner(RotateTransform spinnerRotate)
        {
            DoubleAnimation rotate = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(800))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            spinnerRotate.BeginAnimation(RotateTransform.AngleProperty, rotate);
        }
    }
}