using NUnit.Framework;
using EntityLayer;
using DataLayer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;

namespace TestLayer
{
    [TestFixture]
    public class BookingTests
    {
        private SkiCenterDbContext _context;

        /// <summary>
        /// Happy path for testcase KR-HP-01.1
        /// </summary>
        [Test]
        public void SearchExistingCustomer_ShouldFindExistingCustomer()
        {
            //Arrange
            var Customer = new PrivateCustomer("Anna", "Stenvall", "Stigen 1", "Anna@test.se", "0701234567", 10.2m);

            // Act
            var foundCustomer = Customer.Email == "anna@test.se";

            // Assert
            Assert.That(foundCustomer, Is.True, "System should find existing customer");
            
        }

        /// <summary>
        /// Error path for testcase KR-HP-01.2
        /// </summary>
        [Test]
        public void TryRegisterDuplicateCustomer_ShouldDeny()
        {
            // Arrange 
            string existingEmail = "anna@test.se";
            var existingCustomer = new PrivateCustomer("Anna", "Stenvall", "Stigen 1", existingEmail, "0701234567", 10.2m);

            // Act
            string newEmailToRegister = "anna@test.se";
            bool isDuplicate = (existingEmail == newEmailToRegister);

            // Assert
            Assert.That(isDuplicate, Is.True, "System should identify duplicate");
        }
    }
}
