using BusinessLayer.Controllers;
using DataLayer;
using EntityLayer;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class CustomerRegistryIntegrationTests
    {
        private SkiCenterDbContext _context;
        private UnitOfWork _unitOfWork;
        private CustomerController _customerController;

        [SetUp]
        public void Setup()
        {
            _context = new SkiCenterDbContext();
            _unitOfWork = new UnitOfWork(_context);
            _customerController = new CustomerController(_unitOfWork);
        }

        [TearDown]
        public void TearDown()
        {
            _unitOfWork?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public void RegisterPrivateCustomer_ValidData_ShouldSaveToDatabaseWithDefaultCreditLimit()
        {
            // Arrange
            string testEmail = "integration_privat@test.se";
            string testPhone = "0701239988";

            // Ensure that the test customer is not already left over from an interrupted previous test
            var existing = _context.Customers.FirstOrDefault(c => c.Email == testEmail);
            if (existing != null)
            {
                _context.Customers.Remove(existing);
                _context.SaveChanges();
            }

            // Act
            // Fetch BusinessLayer method in controller.
            bool isRegistered = _customerController.RegisterPrivateCustomer(
                "Kalle",
                "Anka",
                "Paradisäppelvägen 111",
                testEmail,
                testPhone
            );

            // Assert
            // Verify that BusinessLayer is true.
            Assert.That(isRegistered, Is.True, "The method should return true for valid customer.");

            // Verify that DataLayer 
            var savedCustomer = _context.Customers
                .OfType<PrivateCustomer>()
                .FirstOrDefault(c => c.Email == testEmail);

            Assert.That(savedCustomer, Is.Not.Null, "Customer was not found in database.");
            Assert.That(savedCustomer.CustomerID, Is.GreaterThan(0), "The customer did not receive an ID from the database..");

            // Verify creditlimit. 
            Assert.That(savedCustomer.CreditLimit, Is.EqualTo(12000m), "The credit limit will be set to SEK 12,000 automatically.");

            // Clean up
            _context.Customers.Remove(savedCustomer);
            _context.SaveChanges();
        }

        [Test]
        public void RegisterPrivateCustomer_DuplicateEmail_ShouldReturnFalse()
        {
            // Arrange
            string email = "duplicate@test.se";
            string phone = "0700000001";

            _customerController.RegisterPrivateCustomer("Test", "Person", "Gatan 1", email, phone);

            // Act
            bool duplicateResult = _customerController.RegisterPrivateCustomer("Annan", "Person", "Gatan 2", email, "0700000002");

            // Assert
            Assert.That(duplicateResult, Is.False, "System should deny creation of new customer due to email already existing.");

            // Clean up
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
            }
        }

        [Test]
        public void RegisterCorporateCustomer_ValidData_ShouldSaveToDatabaseWithDefaultCreditLimit()
        {
            // Arrange
            string testEmail = "integration_corporate@test.se";
            string testPhone = "0701239988";

            // Ensure that the test customer is not already left over from an interrupted previous test
            var existing = _context.Customers.FirstOrDefault(c => c.Email == testEmail);
            if (existing != null)
            {
                _context.Customers.Remove(existing);
                _context.SaveChanges();
            }

            // Act
            // Fetch BusinessLayer method in controller.
            bool isRegistered = _customerController.RegisterCorporateCustomer(
                "SAP",
                "345689-3454",
                "Mark",
                "Dietmar-Hopp-Allee 16",
                testEmail,
                testPhone
            );

            // Assert
            // Verify that BusinessLayer is true.
            Assert.That(isRegistered, Is.True, "The method should return true for a valid corporate customer.");

            // Retrieve the customer using OfType<CorporateCustomer>
            var savedCustomer = _context.Customers
                .OfType<CorporateCustomer>()
                .FirstOrDefault(c => c.Email == testEmail);

            Assert.That(savedCustomer, Is.Not.Null, "Corporate customer was not found in database.");
            Assert.That(savedCustomer.CustomerID, Is.GreaterThan(0), "Customer did not receive an auto-generated ID.");

            // Validate corporate-specific properties and business rules
            Assert.That(savedCustomer.CompanyName, Is.EqualTo("SAP"), "Company name must match input.");
            Assert.That(savedCustomer.OrganisationNumber, Is.EqualTo("345689-3454"), "Organization number must match input.");
            Assert.That(savedCustomer.ContactPerson, Is.EqualTo("Mark"), "Contact person must match input.");
            Assert.That(savedCustomer.CreditLimit, Is.EqualTo(0m), "Default credit limit should be 0 until approved.");
            Assert.That(savedCustomer.DiscountRate, Is.EqualTo(0m), "Default discount rate should be 0.");
            Assert.That(savedCustomer.IsApproved, Is.False, "New corporate customer must not be approved by default.");

            // Clean up
            _context.Customers.Remove(savedCustomer);
            _context.SaveChanges();
        }

        [Test]
        public void RegisterCorporateCustomer_DuplicateEmail_ShouldReturnFalse()
        {
            // Arrange
            string email = "duplicate@test.se";
            string phone = "0700000001";

            _customerController.RegisterCorporateCustomer("SAP", "345689-3454", "Dietmar-Hopp-Allee 16", "Mark", email, phone);

            // Act
            bool duplicateResult = _customerController.RegisterCorporateCustomer("SAP", "345689-3454", "Dietmar-Hopp-Allee 16", "Mark", email, "0700000002");

            // Assert
            Assert.That(duplicateResult, Is.False, "System should deny creation of new customer due to email already existing.");

            // Clean up
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
            }
        }

        /*
        [Test]
        public void ApproveCreditLimit_MarketingManager_ShouldChangeValue()
        {
            _customerController.ApproveCorporateCustomer()
        }
        */
    }
}