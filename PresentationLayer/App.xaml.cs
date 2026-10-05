using BusinessLayer.Controllers;
using DataLayer;
using DataLayer.Interfaces;
using DataLayer.Repositories;
using Microsoft.Extensions.DependencyInjection;
using PresentationLayer.Views;
using System.Windows;


namespace PresentationLayer
{
    /// <summary>
    /// Represents the main application entry point and acts as the Root.
    /// Responsible for configuring the Dependency Injection (DI) container, registering 
    /// application dependencies, and executing the startup sequence.
    /// </summary>
    public partial class App : Application
    {

        private readonly ServiceProvider _serviceProvider;

        /// <summary>
        /// Provides access to the application's built service provider so that objects
        /// constructed by WPF (via XAML) can still resolve services from the DI container.
        /// </summary>
        public IServiceProvider Services => _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Sets up the service collection, registers all necessary dependencies, 
        /// and builds the final service provider used to resolve instances at runtime.
        /// </summary>
        public App()
        {
            // 1. Create a new service collection
            var services = new ServiceCollection();

            // 2. Configure dependencies
            ConfigureServices(services);

            // 3. Build the provider (the container that holds everything)
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Registers all required views (windows), controllers, repositories, and database contexts 
        /// into the Dependency Injection container with their appropriate lifespans.
        /// </summary>
        /// <param name="services">The collection where application services are registered.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Register Views (Windows)
            services.AddTransient<LoginView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<CustomerRegistrationView>();
            services.AddTransient<EquipmentMgmtView>();

            // 2. Register Controllers
            services.AddTransient<LoginController>();
            services.AddTransient<CustomerController>();
            services.AddTransient<DashboardController>();

            // 3. Register Repositories
            services.AddTransient<ICustomerRepository, CustomerRepository>();

            // 4. Register and initialize DbContext
            services.AddDbContext<SkiCenterDbContext>();

            // Register UnitOfWork so IUnitOfWork can be resolved by controllers and views.
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        /// <summary>
        /// Create the initial window using the DI container instead of letting WPF
        /// instantiate it via StartupUri. This preserves constructor injection.
        /// </summary>
        /// <param name="e">Startup event args.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve the main window (LoginView) from the service provider and show it
            var loginWindow = Services.GetRequiredService<LoginView>();
            loginWindow.Show();

            /*
            // Only use when data should be written from seeding files. I (Jonathan will fix this during this week)
            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SkiCenterDbContext>();
                DbInitializer.Initialize(context);
            }
            */

        }

    }

}
