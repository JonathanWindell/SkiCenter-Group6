using System;
using System.Collections.Generic;
using System.Text;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

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
            optionsBuilder.UseSqlServer(@"Server=sqlutb4-db.hb.se,56077;Database=oosu2614;User ID=siht2606;Password=LWB842;TrustServerCertificate=True;");
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// Create DbSet for every entity
        /// </summary>
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Staff> StaffMembers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingAccomodation> BookingAccomodations { get; set; }
        public DbSet<Accommodation> Accommodations { get; set; }
        public DbSet<SeasonPrice> SeasonPrices { get; set; }
        public DbSet<MeetingRoom> MeetingRooms { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentItem> EquipmentItems { get; set; }
        public DbSet<Rental> Rental { get; set; }
        public DbSet<SkiLesson> SkiLessons { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
    }
}
