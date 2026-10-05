using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Main menu shown after login. Builds one menu button for each system module
    /// the logged in staff member's role is allowed to access.
    /// </summary>
    public partial class DashboardView : Window
    {
        private readonly DashboardController _dashboardController;
        private readonly LoginController _loginController;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Display information for a menu button. WindowType is null for modules that have no view yet.
        /// </summary>
        private record MenuItemInfo(string Title, string Description, Type? WindowType);

        // Connects each module to its button text and the window it opens
        private static readonly Dictionary<SystemModule, MenuItemInfo> _menuItems = new()
        {
            [SystemModule.CustomerRegistration] = new("Registrera kund", "Lägg till privat- eller företagskunder", typeof(CustomerRegistrationView)),
            [SystemModule.EquipmentManagement] = new("Utrustning", "Hantera skidutrustning och lager", typeof(EquipmentMgmtView)),
            [SystemModule.Rentals] = new("Uthyrning", "Hyr ut och ta emot utrustning", null),
            [SystemModule.Bookings] = new("Bokningar", "Skapa och hantera bokningar", null),
            [SystemModule.Accommodation] = new("Boende", "Lägenheter, stugor och konferensrum", null),
            [SystemModule.SkiSchool] = new("Skidskola", "Lektioner och gruppbokningar", null),
            [SystemModule.SeasonPrices] = new("Säsongspriser", "Ändra priser per säsong", null),
            [SystemModule.Reports] = new("Rapporter", "Statistik och beläggning", null),
            [SystemModule.StaffAdmin] = new("Personal", "Hantera personal och roller", null)
        };

        // Swedish display names for staff roles
        private static readonly Dictionary<staffRole, string> _roleNames = new()
        {
            [staffRole.SkiShop] = "Skidbutik",
            [staffRole.MarketingManager] = "Marknadschef",
            [staffRole.BookingAdmin] = "Bokningsadministratör",
            [staffRole.SystemAdmin] = "Systemadministratör"
        };

        public DashboardView(DashboardController dashboardController, LoginController loginController, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _dashboardController = dashboardController;
            _loginController = loginController;
            _serviceProvider = serviceProvider;

            ShowUserInfo();
            BuildMenu();
        }

        /// <summary>
        /// Shows the logged in staff member's name and role in the header.
        /// </summary>
        private void ShowUserInfo()
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null)
            {
                return;
            }

            txtWelcome.Text = $"Välkommen, {user.FirstName}";
            txtRole.Text = _roleNames.TryGetValue(user.Role, out string? roleName) ? roleName : user.Role.ToString();
        }

        /// <summary>
        /// Creates one menu button for every module the current user has access to.
        /// </summary>
        private void BuildMenu()
        {
            MenuPanel.Children.Clear();

            IReadOnlyList<SystemModule> modules = _dashboardController.GetModulesForCurrentUser();

            foreach (SystemModule module in modules)
            {
                MenuPanel.Children.Add(CreateMenuButton(module));
            }

            txtNoModules.Visibility = modules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private Button CreateMenuButton(SystemModule module)
        {
            MenuItemInfo info = _menuItems[module];

            var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            content.Children.Add(new TextBlock
            {
                Text = info.Title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("PrimaryText"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = info.Description,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("SecondaryText")
            });

            var button = new Button
            {
                Content = content,
                Width = 240,
                Height = 130,
                Style = (Style)FindResource("MenuCardStyle"),
                Tag = module
            };

            button.Click += MenuButton_Click;
            return button;
        }

        /// <summary>
        /// Opens the window for the clicked module after re-checking access.
        /// </summary>
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: SystemModule module })
            {
                return;
            }

            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !DashboardController.HasAccess(user.Role, module))
            {
                MessageBox.Show("Du har inte behörighet till denna vy.");
                return;
            }

            Type? windowType = _menuItems[module].WindowType;

            if (windowType == null)
            {
                MessageBox.Show("Denna vy är inte klar än.");
                return;
            }

            var window = (Window)_serviceProvider.GetRequiredService(windowType);
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Logs out the current user and returns to the login window at the same position.
        /// </summary>
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            _loginController.Logout();

            LoginView loginView = _serviceProvider.GetRequiredService<LoginView>();
            loginView.WindowStartupLocation = WindowStartupLocation.Manual;
            loginView.Left = this.Left;
            loginView.Top = this.Top;

            loginView.Show();
            this.Close();
        }

        /// <summary>
        /// Allows the user to move the window by clicking and dragging the background.
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
