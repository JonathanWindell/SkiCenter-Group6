using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PresentationLayer.Views;
using BusinessLayer.Controllers;


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

            services.AddTransient<CustomerRegistrationView>();

            // 2. Register Controllers
            services.AddTransient<LoginController>();

            // 3. Register Repositories
           // services.AddTransient<IStaffRepository, StaffRepository>();
           // services.AddTransient<IResourceRepository, ResourceRepository>();

            // 4. Register and initialize DbContext
          //  services.AddDbContext<LabDbContext>();

            // Register UnitOfWork so IUnitOfWork can be resolved
           // services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

    }

}
