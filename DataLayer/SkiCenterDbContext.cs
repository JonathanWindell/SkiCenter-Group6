using EntityLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataLayer
{
    public class SkiCenterDbContext : DbContext
    {
        /* File contains OnConfiguring method. Imported by DbContext. Part of EF core SQLServer. 
         * File contains DbSets used for saving instances of entites. 
         * Creates connection to database using connection string. 
        */
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Creates config and poins to appsettings.json 
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Fetch string with name "DefaultConnection". 
                string connectionString = configuration.GetConnectionString("DefaultConnection");

                // Connect to database.
                optionsBuilder.UseSqlServer(connectionString);
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Ensures standard value for decimals. 
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }

        /// <summary>
        /// Create DbSet for every entity
        /// </summary>
        public DbSet<Customer> Customers { get; set; }
        public DbSet<PrivateCustomer> PrivateCustomers { get; set; }
        public DbSet<CorporateCustomer> CorporateCustomers { get; set; }
        public DbSet<Staff> StaffMembers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingAccommodation> BookingAccommodations { get; set; }
        public DbSet<Accommodation> Accommodations { get; set; }
        public DbSet<SeasonPrice> SeasonPrices { get; set; }
        public DbSet<MeetingRoom> MeetingRooms { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentItem> EquipmentItems { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<SkiLesson> SkiLessons { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
    }
}
