using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class MarketingMgmtUnitTests
    {
        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ICustomerRepository> _mockCustomerRepo;
        private Mock<IBookingRepository> _mockBookingRepo;
        private CustomerController _customerController;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();

            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);

            _customerController = new CustomerController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Query Tests (Read)

        /// <summary>
        /// Verifies that GetAllCorporateCustomers fetches all corporate customer entities directly from the repository.
        /// </summary>
        [Test]
        public void GetAllCorporateCustomers_ReturnsAllCorporateCustomersFromRepository()
        {
            // Arrange
            var expectedCustomers = new List<CorporateCustomer>
            {
                new CorporateCustomer("Bikesters AB", "556111-2233", "Johan Jonshede", "Storgatan 1", "info@bikesters.se", "0701112233")
                {
                    CustomerID = 1,
                    IsApproved = true
                },
                new CorporateCustomer("Torkels Pyssel AB", "418502-7832", "Torkel Jansson", "Pysselvägen 2", "info@pyssel.se", "0704445566")
                {
                    CustomerID = 2,
                    IsApproved = false
                }
            };

            _mockCustomerRepo.Setup(r => r.GetAllCorporateCustomers())
                             .Returns(expectedCustomers);

            // Act
            var result = _customerController.GetAllCorporateCustomers();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCustomers));
            _mockCustomerRepo.Verify(r => r.GetAllCorporateCustomers(), Times.Once);
        }

        #endregion

        #region Business Logic & State Transitions

        /// <summary>
        /// Verifies that transitioning an unapproved customer to approved updates agreement terms, 
        /// normalizes the discount rate from percentage to decimal, and confirms preliminary bookings.
        /// </summary>
        [Test]
        public void UpdateCorporateCustomer_WhenApprovingCustomer_UpdatesTermsConvertsDiscountAndConfirmsPreliminaryBookings()
        {
            // Arrange
            int customerId = 5;
            decimal creditLimit = 50000m;
            decimal discountPercent = 10m; // 10% should be stored as 0.10m

            var customer = new CorporateCustomer("Bikesters AB", "556111-2233", "Johan Jonshede", "Storgatan 1", "info@bikesters.se", "0701112233")
            {
                CustomerID = customerId,
                IsApproved = false,
                CreditLimit = 0m,
                DiscountRate = 0m
            };

            var customerBookings = new List<Booking>
            {
                new Booking { BookingID = 101, CustomerID = customerId, BookingStatus = BookingStatus.Preliminary },
                new Booking { BookingID = 102, CustomerID = customerId, BookingStatus = BookingStatus.Cancelled }
            };

            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns(customer);
            _mockBookingRepo.Setup(r => r.GetBookingsByCustomer(customerId)).Returns(customerBookings);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _customerController.UpdateCorporateCustomer(customerId, isApproved: true, creditLimit, discountPercent);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(customer.IsApproved, Is.True);
            Assert.That(customer.CreditLimit, Is.EqualTo(creditLimit));
            Assert.That(customer.DiscountRate, Is.EqualTo(0.10m));

            // Verify preliminary booking changed to Confirmed, while Cancelled remained untouched
            Assert.That(customerBookings[0].BookingStatus, Is.EqualTo(BookingStatus.Confirmed));
            Assert.That(customerBookings[1].BookingStatus, Is.EqualTo(BookingStatus.Cancelled));

            _mockCustomerRepo.Verify(r => r.Update(customer), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that modifying agreement terms for an already approved customer updates details 
        /// without querying or modifying bookings again.
        /// </summary>
        [Test]
        public void UpdateCorporateCustomer_AlreadyApproved_UpdatesTermsWithoutQueryingBookings()
        {
            // Arrange
            int customerId = 5;
            decimal creditLimit = 80000m;
            decimal discountPercent = 5m; // 5% should become 0.05m

            var customer = new CorporateCustomer("Bikesters AB", "556111-2233", "Johan Jonshede", "Storgatan 1", "info@bikesters.se", "0701112233")
            {
                CustomerID = customerId,
                IsApproved = true,
                CreditLimit = 50000m,
                DiscountRate = 0.10m
            };

            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns(customer);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _customerController.UpdateCorporateCustomer(customerId, isApproved: true, creditLimit, discountPercent);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(customer.IsApproved, Is.True);
            Assert.That(customer.CreditLimit, Is.EqualTo(creditLimit));
            Assert.That(customer.DiscountRate, Is.EqualTo(0.05m));

            // Booking repository must not be invoked since approval status was not newly granted
            _mockBookingRepo.Verify(r => r.GetBookingsByCustomer(It.IsAny<int>()), Times.Never);
            _mockCustomerRepo.Verify(r => r.Update(customer), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that setting IsApproved to false revokes approval and saves the new state.
        /// </summary>
        [Test]
        public void UpdateCorporateCustomer_RevokeApproval_SetsIsApprovedToFalseAndUpdatesTerms()
        {
            // Arrange
            int customerId = 6;
            decimal creditLimit = 30000m;
            decimal discountPercent = 0m;

            var customer = new CorporateCustomer("Kristna Budskapet AB", "512358-1832", "Tommy Klaesson", "Vägen 5", "tommy@budskapet.se", "0709998877")
            {
                CustomerID = customerId,
                IsApproved = true,
                CreditLimit = 30000m,
                DiscountRate = 0.05m
            };

            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns(customer);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _customerController.UpdateCorporateCustomer(customerId, isApproved: false, creditLimit, discountPercent);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(customer.IsApproved, Is.False);
            Assert.That(customer.CreditLimit, Is.EqualTo(creditLimit));
            Assert.That(customer.DiscountRate, Is.EqualTo(0m));

            _mockCustomerRepo.Verify(r => r.Update(customer), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Validation & Edge Cases (Sad Paths)

        /// <summary>
        /// Verifies that validation prevents execution and returns false when invalid arguments are provided.
        /// </summary>
        [Test]
        [TestCase(-1, true, 10000, 10)]   // Negative ID
        [TestCase(0, true, 10000, 10)]    // Zero ID
        [TestCase(1, true, -100, 10)]     // Negative credit limit
        [TestCase(1, true, 10000, -1)]    // Negative discount
        [TestCase(1, true, 10000, 101)]   // Discount above 100%
        public void UpdateCorporateCustomer_InvalidInputs_ReturnsFalseWithoutCallingDatabase(
            int customerId, bool isApproved, decimal creditLimit, decimal discountRate)
        {
            // Act
            bool result = _customerController.UpdateCorporateCustomer(customerId, isApproved, creditLimit, discountRate);

            // Assert
            Assert.That(result, Is.False);

            _mockCustomerRepo.Verify(r => r.GetByID(It.IsAny<int>()), Times.Never);
            _mockCustomerRepo.Verify(r => r.Update(It.IsAny<CorporateCustomer>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that the method returns false when the specified customer ID does not exist in the database.
        /// </summary>
        [Test]
        public void UpdateCorporateCustomer_CustomerNotFound_ReturnsFalse()
        {
            // Arrange
            int customerId = 999;
            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns((Customer)null!);

            // Act
            bool result = _customerController.UpdateCorporateCustomer(customerId, isApproved: true, 25000m, 5m);

            // Assert
            Assert.That(result, Is.False);

            _mockCustomerRepo.Verify(r => r.Update(It.IsAny<CorporateCustomer>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        #endregion
    }
}