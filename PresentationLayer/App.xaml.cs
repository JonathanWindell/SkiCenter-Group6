using System.Windows;
using BusinessLayer;
using BusinessLayer.Controllers;
using DataLayer;
using DataLayer.Interfaces;
using DataLayer.Repositories;
using Microsoft.Extensions.DependencyInjection;
using PresentationLayer.Views;

namespace PresentationLayer
{
    /// <summary>
    /// Represents the main application entry point and acts as the Root.
    /// Responsible for configuring the Dependency Injection (DI) container,
    /// registering application dependencies, and executing the startup sequence.
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        /// <summary>
        /// Provides access to the application's built service provider.
        /// </summary>
        public IServiceProvider Services => _serviceProvider;

        /// <summary>
        /// Initializes the application and configures dependency injection.
        /// </summary>
        public App()
        {
            // Create a new service collection.
            var services = new ServiceCollection();

            // Register application dependencies.
            ConfigureServices(services);

            // Build the service provider.
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Registers views, controllers, repositories, and database services.
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Register Views (Windows)
            services.AddTransient<LoginView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<CustomerRegistrationView>();
            services.AddTransient<EquipmentMgmtView>();
            services.AddTransient<CustomerBookingsView>();
            services.AddTransient<CreateBookingView>();
            services.AddTransient<BookingDetailsView>();
            services.AddTransient<SeasonPriceView>();
            services.AddTransient<GuestCheckInOutView>();
            services.AddTransient<BookingDetailsView>();
            services.AddTransient<BookingStatusView>();


            // 2. Register Controllers
            services.AddTransient<LoginController>();
            services.AddTransient<BookingController>();
            services.AddTransient<CustomerController>();
            services.AddTransient<DashboardController>();
            services.AddTransient<SkiShopController>();
            services.AddTransient<AdminController>();

            // 3. Register Repositories
            services.AddTransient<ICustomerRepository, CustomerRepository>();

            // 4. Register and initialize DbContext
            services.AddDbContext<SkiCenterDbContext>();

            // Register UnitOfWork for controllers and views.
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        /// <summary>
        /// Opens the login window when the application starts.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve the main window (LoginView) from the service provider and show it
            var loginWindow = Services.GetRequiredService<LoginView>();
            loginWindow.Show();

            // To test the guest view directly without logging in, comment out the two lines above and use:
            // var guestWindow = Services.GetRequiredService<GuestCheckInOutView>();
            // guestWindow.Show();

            /*
            // Only use when data should be written from seeding files.
            // Jonathan will handle database initialization.

            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<SkiCenterDbContext>();

                DbInitializer.Initialize(context);
            }
            */
        }
    }
}
