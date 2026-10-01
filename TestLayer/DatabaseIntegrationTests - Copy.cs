using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using DataLayer;
using EntityLayer;
using BusinessLayer;
using NUnit.Framework;

namespace TestLayer
{
    [TestFixture]
    public class DatabaseIntegrationTests
    {
        private SkiCenterDbContext _context;

        [SetUp]
        public void Setup()
        {
            // Initialize database. 
            _context = new SkiCenterDbContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure the DbContext is disposed after each test to avoid resource leaks
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        [Test]
        public void AddPrivateCustomer_ShouldSaveToDatabase()
        {
            // Arrange
            var newCustomer = new PrivateCustomer("Anna", "Stenvall", "Stigen 1", "Anna@test.se", "0701234567", 10.2m);

            // Act
            // CanConnect() opens a connectiona and tries to connect. 
            // Returns true if connectionstring is valid.
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            // Assert
            var savedCustomer = _context.Customers.FirstOrDefault(c => c.Email == "Anna@test.se");

            Assert.That(savedCustomer, Is.Not.Null);
            Assert.That(savedCustomer.CustomerID, Is.GreaterThan(0), "Customer was not saved in the database");

            // If testdata should be removed. Uncomment this. 
            _context.Customers.Remove(savedCustomer);
            _context.SaveChanges();
        }

        [Test] 
        public void AddStaff_ShouldSaveToDatabase()
        {
            // Arrange
            string inputPassword = "TestBookingAdminPassword";
            string hashedPassword = HashingPassword.HashPassword(inputPassword);

            var newStaff = new Staff("Vincent", "Adolfsson", "BookingAdmin@Test.se", hashedPassword, staffRole.BookingAdmin);

            // Act
            _context.StaffMembers.Add(newStaff);
            _context.SaveChanges();

            var savedStaff = _context.StaffMembers.FirstOrDefault(s => s.Email == "BookingAdmin@Test.se");

            // Assert
            Assert.That(savedStaff, Is.Not.Null);
            Assert.That(savedStaff.StaffID, Is.GreaterThan(0), "Staff was not saved in the database.");

            // If testdata should be removed. Uncomment this. 
            /*
            _context.StaffMembers.Remove(savedStaff);
            _context.SaveChanges();
            */
        }
    }
}
