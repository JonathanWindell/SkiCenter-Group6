using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Main menu shown after login. Builds one menu button for each system module
    /// the logged in staff member's role is allowed to access.
    /// </summary>
    public partial class DashboardView : Window
    {
        #region Fields and constructor

        private readonly DashboardController _dashboardController;
        private readonly LoginController _loginController;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Display information for a menu card. WindowType is null for modules that have no view yet.
        /// IconKey is the icon's resource key in Resources/Icons.xaml.
        /// </summary>
        private record MenuItemInfo(string Title, string Description, string IconKey, Type? WindowType);

        // Connects each module to its card text, icon and the window it opens
        private static readonly Dictionary<SystemModule, MenuItemInfo> _menuItems = new()
        {
            [SystemModule.CustomerRegistration] = new("Registrera kund", "Lägg till privat- eller företagskunder", "Icon.RegistreraKund", typeof(CustomerRegistrationView)),
            [SystemModule.EquipmentManagement] = new("Utrustning", "Hantera skidutrustning och lager", "Icon.Utrustning", typeof(EquipmentMgmtView)),
            [SystemModule.Rentals] = new("Uthyrning", "Lägg till utrustning på en bokning", "Icon.Uthyrning", typeof(EquipmentRentalView)),
            [SystemModule.Bookings] = new("Bokningar", "Skapa och hantera bokningar", "Icon.Bokningar", typeof(BookingStatusView)),
            [SystemModule.Accommodation] = new("Boende", "Lägenheter, stugor och konferensrum", "Icon.Boende", typeof(PropertyMgmtView)),
            [SystemModule.SkiSchool] = new("Skidskola", "Lektioner och gruppbokningar", "Icon.Skidskola", typeof(SkiSchoolView)),
            [SystemModule.SeasonPrices] = new("Säsongspriser", "Ändra priser per säsong", "Icon.Sasongspriser", typeof(SeasonPriceView)),
            [SystemModule.Reports] = new("Rapporter", "Statistik och beläggning", "Icon.Rapporter", typeof(StatisticsView)),
            [SystemModule.StaffAdmin] = new("Personal", "Hantera personal och roller", "Icon.Personal", typeof(StaffAdminView)),
            [SystemModule.GuestCheckInOut] = new("In- och utcheckning", "Hantera gästers ankomst och avresa", "Icon.InUtcheckning", typeof(GuestCheckInOutView)),
            [SystemModule.InvoiceExport] = new("Faktureringsunderlag", "Exportera avslutade bokningar som JSON", "Icon.Fakturering", typeof(InvoiceExportView)),
            [SystemModule.MarketingManager] = new("Marknad", "Besluta om rabatter och godkänn företagskunder", "Icon.Marknad", typeof(MarketingMgmtView))
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

        #endregion

        #region Menu

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
            txtRole.Text = StaffController.GetRoleDisplayName(user.Role);
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

        /// <summary>
        /// Creates a menu card: the module's icon in a circle, the title and a short description.
        /// The round arrow on the right comes from DashboardCardStyle.
        /// </summary>
        private Button CreateMenuButton(SystemModule module)
        {
            MenuItemInfo info = _menuItems[module];

            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon drawn on a 64 x 64 canvas, like the SVG originals
            var icon = new Path
            {
                Data = (Geometry)FindResource(info.IconKey),
                Stroke = (Brush)FindResource("PrimaryBtn"),
                Style = (Style)FindResource("IconPathStyle")
            };
            var canvas = new Canvas { Width = 64, Height = 64 };
            canvas.Children.Add(icon);

            var iconCircle = new Border
            {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(23),
                Background = (Brush)FindResource("IconCircleBg"),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new Viewbox { Width = 24, Height = 24, Child = canvas }
            };
            content.Children.Add(iconCircle);

            var text = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            text.Children.Add(new TextBlock
            {
                Text = info.Title,
                FontSize = 13,
                FontFamily = (FontFamily)FindResource("HeadingFont"),
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("PrimaryText"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            text.Children.Add(new TextBlock
            {
                Text = info.Description,
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 32,
                Foreground = (Brush)FindResource("MutedText")
            });

            Grid.SetColumn(text, 1);
            content.Children.Add(text);

            var button = new Button
            {
                Content = content,
                Style = (Style)FindResource("DashboardCardStyle"),
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

        #endregion

        #region Logout and window controls

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

        #endregion
    }
}
