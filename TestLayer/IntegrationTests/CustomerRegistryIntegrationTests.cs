using BusinessLayer.Controllers;
using DataLayer;
using EntityLayer;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class CustomerRegistryIntegrationTests
    {
        private SkiCenterDbContext _context;
        private CustomerController _customerController;

        // Tracks created customer emails to guarantee cleanup in TearDown even if an assertion fails
        private List<string> _createdCustomerEmails;

        [SetUp]
        public void Setup()
        {
            _context = new SkiCenterDbContext();

            // Wire up dependencies locally to avoid NUnit1032 warnings
            var unitOfWork = new UnitOfWork(_context);
            _customerController = new CustomerController(unitOfWork);

            _createdCustomerEmails = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                // Remove all customers created during the test run
                if (_createdCustomerEmails.Any())
                {
                    var customersToRemove = _context.Customers
                        .Where(c => _createdCustomerEmails.Contains(c.Email))
                        .ToList();

                    if (customersToRemove.Any())
                    {
                        _context.Customers.RemoveRange(customersToRemove);
                        _context.SaveChanges();
                    }
                }

                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        /// Test-ID: KR-HP-01.1
        /// Verifies that a private customer is registered successfully, persisted in the database,
        /// and assigned the default credit limit of 12 000 SEK.
        /// </summary>
        [Test]
        public void PrivateCustomer_ValidData_ReturnsTrue()
        {
            // Arrange
            string email = "integration_privat_valid@test.se";
            string phone = "0701239988";
            _createdCustomerEmails.Add(email);

            // Act
            bool isRegistered = _customerController.RegisterPrivateCustomer(
                "Kalle",
                "Anka",
                "Paradisäppelvägen 111",
                email,
                phone
            );

            // Assert
            Assert.That(isRegistered, Is.True, "The method should return true for valid customer input.");

            var savedCustomer = _context.Customers
                .OfType<PrivateCustomer>()
                .FirstOrDefault(c => c.Email == email);

            Assert.That(savedCustomer, Is.Not.Null, "Customer was not found in the database.");
            Assert.That(savedCustomer.CustomerID, Is.GreaterThan(0), "Customer did not receive an auto-generated ID.");
            Assert.That(savedCustomer.CreditLimit, Is.EqualTo(12000m), "Default credit limit must be set to 12 000 SEK.");
        }

        /// <summary>
        /// Test-ID: KR-EP-01.2
        /// Verifies that registering a private customer with an already existing email is rejected.
        /// </summary>
        [Test]
        public void PrivateCustomer_DuplicateEmail_ReturnsFalse()
        {
            // Arrange
            string email = "duplicate_privat@test.se";
            _createdCustomerEmails.Add(email);

            _customerController.RegisterPrivateCustomer("Test", "Person", "Gatan 1", email, "0700000001");

            // Act: Attempt to register another customer with identical email
            bool duplicateResult = _customerController.RegisterPrivateCustomer("Annan", "Person", "Gatan 2", email, "0700000002");

            // Assert
            Assert.That(duplicateResult, Is.False, "System should deny registration when email already exists.");
        }

        /// <summary>
        /// Test-ID: KR-HP-01.3
        /// Verifies that a corporate customer is registered with status unapproved (IsApproved = false)
        /// and initial credit limit of 0 SEK until approved by management.
        /// </summary>
        [Test]
        public void CorporateCustomer_ValidData_ReturnsTrue()
        {
            // Arrange
            string email = "integration_corporate_valid@test.se";
            string phone = "0701239988";
            _createdCustomerEmails.Add(email);

            // Act
            bool isRegistered = _customerController.RegisterCorporateCustomer(
                "SAP",
                "345689-3454",
                "Mark",
                "Dietmar-Hopp-Allee 16",
                email,
                phone
            );

            // Assert
            Assert.That(isRegistered, Is.True, "The method should return true for valid corporate customer input.");

            var savedCustomer = _context.Customers
                .OfType<CorporateCustomer>()
                .FirstOrDefault(c => c.Email == email);

            Assert.That(savedCustomer, Is.Not.Null, "Corporate customer was not found in the database.");
            Assert.That(savedCustomer.CustomerID, Is.GreaterThan(0), "Customer did not receive an auto-generated ID.");
            Assert.That(savedCustomer.CompanyName, Is.EqualTo("SAP"));
            Assert.That(savedCustomer.OrganisationNumber, Is.EqualTo("345689-3454"));
            Assert.That(savedCustomer.ContactPerson, Is.EqualTo("Mark"));
            Assert.That(savedCustomer.CreditLimit, Is.EqualTo(0m), "Default credit limit should be 0 until approved.");
            Assert.That(savedCustomer.DiscountRate, Is.EqualTo(0m), "Default discount rate should be 0.");
            Assert.That(savedCustomer.IsApproved, Is.False, "New corporate customer must not be approved by default.");
        }

        /// <summary>
        /// Test-ID: KR-EP-01.4
        /// Verifies that registering a corporate customer with an existing email is denied.
        /// </summary>
        [Test]
        public void CorporateCustomer_DuplicateEmail_ReturnsFalse()
        {
            // Arrange
            string email = "duplicate_corporate@test.se";
            _createdCustomerEmails.Add(email);

            _customerController.RegisterCorporateCustomer("SAP", "345689-3454", "Mark", "Dietmar-Hopp-Allee 16", email, "0700000001");

            // Act: Attempt registration with identical email
            bool duplicateResult = _customerController.RegisterCorporateCustomer("SAP 2", "345689-3454", "Dietmar", "Storgatan 1", email, "0700000002");

            // Assert
            Assert.That(duplicateResult, Is.False, "System should deny corporate registration when email already exists.");
        }

        /// <summary>
        /// Test-ID: KR-HP-01.5
        /// Verifies that approving an existing corporate customer updates status to approved
        /// and sets the agreed credit limit and discount rate in the database.
        /// </summary>
        [Test]
        public void UnapprovedCorporateCustomer_ApproveCustomer_ReturnsTrue()
        {
            // Arrange: Seed an unapproved corporate customer
            string email = "corporate_approve@test.se";
            _createdCustomerEmails.Add(email);

            _customerController.RegisterCorporateCustomer("TechCorp", "556677-8899", "Anna", "Teknikvägen 1", email, "0701122334");

            var customer = _context.Customers.OfType<CorporateCustomer>().FirstOrDefault(c => c.Email == email);
            Assert.That(customer, Is.Not.Null, "Precondition failed: Corporate customer must exist.");

            decimal approvedCreditLimit = 50000m;
            decimal approvedDiscount = 0.10m; // 10% discount applied

            // Act: Approve the corporate customer through controller
            bool isApproved = _customerController.ApproveCorporateCustomer(customer.CustomerID, approvedCreditLimit, approvedDiscount);

            // Assert: Verify state changes persisted in the database
            Assert.That(isApproved, Is.True, "ApproveCorporateCustomer should return true.");

            // Reload fresh entity state from database context
            _context.Entry(customer).Reload();

            Assert.That(customer.IsApproved, Is.True, "Customer approval flag must be true.");
            Assert.That(customer.CreditLimit, Is.EqualTo(approvedCreditLimit), "Credit limit must be updated upon approval.");
            Assert.That(customer.DiscountRate, Is.EqualTo(approvedDiscount), "Discount rate must be updated upon approval.");
        }
    }
}