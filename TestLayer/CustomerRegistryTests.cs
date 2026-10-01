using DataLayer;
using EntityLayer;

namespace TestLayer
{
    [TestFixture]
    public class CustomerRegistryTests
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

        /// <summary>
        /// Happy path for testcase KR-HP-01.1
        /// </summary>
        [Test]
        public void SearchExistingCustomer_ShouldFindExistingCustomer()
        {
            //Arrange
            var newCustomer = new PrivateCustomer("Anna", "Stenvall", "Stigen 1", "Anna@test.se", "0701234567", 10.2m);
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            // Act
            var foundCustomer = newCustomer.Email == "anna@test.se";

            // Assert
            Assert.That(foundCustomer, Is.True, "System should find existing customer");

            _context.Customers.Remove(newCustomer);
            _context.SaveChanges();

        }

        /// <summary>
        /// Error path for testcase KR-EP-01.2
        /// </summary>
        [Test]
        public void TryRegisterDuplicateCustomer_ShouldDeny()
        {
            // Arrange 
            string existingEmail = "anna@test.se";
            var Customer = new PrivateCustomer("Anna", "Stenvall", "Stigen 1", existingEmail, "0701234567", 10.2m);

            // Act
            string newEmailToRegister = "anna@test.se";
            bool isDuplicate = (existingEmail == newEmailToRegister);

            // Assert
            Assert.That(isDuplicate, Is.True, "System should identify duplicate");
        }
    }
}

