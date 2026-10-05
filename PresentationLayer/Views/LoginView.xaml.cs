
using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using PresentationLayer.Resources.Animations;
using System.Windows;
using System.Windows.Input;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    /// 
    public partial class LoginView : Window
    {
        private readonly LoginController _loginController;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the login window.
        /// </summary>
        /// <param name="loginController">Controller for authentication operations.</param>
        /// <param name="serviceProvider">Used to resolve subsequent windows and other dependencies.</param>
        public LoginView(LoginController loginController, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _loginController = loginController;

            // Required for XAML bindings to resolve against the controller's properties
            this.DataContext = _loginController;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Allows the user to move the window by clicking and dragging the background.
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        /*
        private void btnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            StartSignUpTransition();
        }
        */

        /// <summary>
        /// Validates credentials against the controller and transitions to the main menu on success,
        /// or displays an error message on failure.
        /// </summary>
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            string password = txtPassword.Password;

            await ElementAnimations.ToggleLoadingAnimationAsync(true, LoginControls, LoadingVisual, SpinnerRotate);

            // Run on a background thread to keep the UI responsive during the network/DB call
            var loginResult = await Task.Run(() => _loginController.Login(email, password));

            await Task.Delay(1000);

            (Staff? staff, string statusMsg) = loginResult;

            if (staff == null)
            {
                await ElementAnimations.ToggleLoadingAnimationAsync(false, LoginControls, LoadingVisual, SpinnerRotate);
                MessageBox.Show(statusMsg);
                return;
            }

            StartWindowTransition();
        }

        /// <summary>
        /// Fades out the login UI, expands the window canvas, then opens the sign-up window
        /// at the same screen position before closing this one.
        /// </summary>
        /*
        private async void StartSignUpTransition()
        {
            double targetWidth = 380;
            double targetHeight = 600;

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            LoginControls.BeginAnimation(OpacityProperty, fadeOut);

            // Allow the fade to complete before expanding the window
            await Task.Delay(200);

            await WindowAnimations.ExpandWindowAsync(MainBorder, targetWidth, targetHeight);

            SignUpWindow signUpWindow = _serviceProvider.GetRequiredService<SignUpWindow>();
            signUpWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            // Match position to the expanded login window so the transition appears seamless
            signUpWindow.Left = this.Left;
            signUpWindow.Top = this.Top;

            signUpWindow.Show();
            this.Close();
        }
        */

        /// <summary>
        /// Expands the window canvas and opens the main menu at the same screen position
        /// before closing this one.
        /// </summary>
        private async void StartWindowTransition()
        {
            double targetWidth = 900;
            double targetHeight = 700;

            await WindowAnimations.ExpandWindowAsync(MainBorder, targetWidth, targetHeight);

            DashboardView dashboard = _serviceProvider.GetRequiredService<DashboardView>();
            dashboard.WindowStartupLocation = WindowStartupLocation.Manual;

            // Match position so the transition appears seamless
            dashboard.Left = this.Left;
            dashboard.Top = this.Top;

            dashboard.Show();
            this.Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
