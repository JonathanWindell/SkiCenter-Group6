using BusinessLayer;
using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class MarketingMgmtUnitTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ICustomerRepository> _mockCustomerRepo;
        private Mock<IBookingRepository> _mockBookingRepo;
        private CustomerController _customerController;
        private BookingController _bookingController;

        [SetUp]
        public void Setup()
        {
            // Create mocks for both Unit of Work and each of the repoitories
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();

            // Configure UnitOfWork to return the mocked repositories
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);

            // Inject the mocked IUnitOfWork into the controllers
            _customerController = new CustomerController(_mockUnitOfWork.Object);
            _bookingController = new BookingController(_mockUnitOfWork.Object);
        }

        /// <summary>
        /// Verifies that GetPendingCorporateCustomers gets the list from repo
        /// </summary>
        [Test]
        public void GetPendingCorporateCustomers_ReturnsPendingListFromRepository()
        {
            // Arrange
            var expectedCustomers = new List<CorporateCustomer>
            {
                new CorporateCustomer("Bikesters AB", "556111-2233", "Johan Jonshede", "Storgatan 1", "info@bikesters.se", "0701112233")
                {
                    CustomerID = 1,
                    IsApproved = false
                }
            };

            _mockCustomerRepo.Setup(r => r.GetPendingCorporateCustomer())
                             .Returns(expectedCustomers);

            // Act
            var result = _customerController.GetPendingCorporateCustomers();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCustomers));
            _mockCustomerRepo.Verify(r => r.GetPendingCorporateCustomer(), Times.Once);
        }

        /// <summary>
        /// Verifies that approving a corp. customer with valid values updates the entity and saves to Db
        /// </summary>
        [Test]
        public void ApproveCorporateCustomer_ValidData_UpdatesCustomerAndReturnsTrue()
        {
            // Arrange
            int customerId = 5;
            decimal creditLimit = 50000m;
            decimal discountRate = 10m;

            var customer = new CorporateCustomer("Bikesters AB", "556111-2233", "Johan Jonshede", "Storgatan 1", "info@bikesters.se", "0701112233")
            {
                CustomerID = customerId,
                IsApproved = false,
                CreditLimit = 0m,
                DiscountRate = 0m
            };

            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns(customer);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _customerController.ApproveCorporateCustomer(customerId, creditLimit, discountRate);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(customer.IsApproved, Is.True);
            Assert.That(customer.CreditLimit, Is.EqualTo(creditLimit));
            Assert.That(customer.DiscountRate, Is.EqualTo(discountRate));

            // Verify
            _mockCustomerRepo.Verify(r => r.Update(customer), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that invalid values aborts the call and no changes are made
        /// </summary>
        [Test]
        [TestCase(-1, 10000, 10)]   // Invalid ID
        [TestCase(0, 10000, 10)]    // ID is 0
        [TestCase(1, -500, 10)]     // Negative creditlimit
        [TestCase(1, 10000, -1)]    // Negative discount
        [TestCase(1, 10000, 101)]   // Discount over 100%
        public void ApproveCorporateCustomer_InvalidInputs_ReturnsFalse(int customerId, decimal creditLimit, decimal discountRate)
        {
            // Act
            bool result = _customerController.ApproveCorporateCustomer(customerId, creditLimit, discountRate);

            // Assert
            Assert.That(result, Is.False);

            // Verify
            _mockCustomerRepo.Verify(r => r.GetByID(It.IsAny<int>()), Times.Never);
            _mockCustomerRepo.Verify(r => r.Update(It.IsAny<CorporateCustomer>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that the method returns false if the customer is not found
        /// </summary>
        [Test]
        public void ApproveCorporateCustomer_CustomerNotFound_ReturnsFalse()
        {
            // Arrange
            int customerId = 99;
            _mockCustomerRepo.Setup(r => r.GetByID(customerId)).Returns((Customer)null);

            // Act
            bool result = _customerController.ApproveCorporateCustomer(customerId, 20000m, 5m);

            // Assert
            Assert.That(result, Is.False);

            // Verify
            _mockCustomerRepo.Verify(r => r.Update(It.IsAny<CorporateCustomer>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that confirmation of preliminary bookings only confirms preliminary bookings, changes to Confirmed
        /// </summary>
        [Test]
        public void ConfirmPreliminaryBookingsForCustomer_ValidCustomer_UpdatesOnlyPreliminaryBookings()
        {
            // Arrange
            int customerId = 1;
            var bookings = new List<Booking>
            {
                new Booking { BookingID = 10, CustomerID = customerId, BookingStatus = BookingStatus.Preliminary },
                new Booking { BookingID = 11, CustomerID = customerId, BookingStatus = BookingStatus.Preliminary },
                new Booking { BookingID = 12, CustomerID = customerId, BookingStatus = BookingStatus.Cancelled }
            };

            _mockBookingRepo.Setup(r => r.GetBookingsByCustomer(customerId)).Returns(bookings);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(2);

            // Act
            int updatedCount = _bookingController.ConfirmPreliminaryBookingsForCustomer(customerId);

            // Assert
            Assert.That(updatedCount, Is.EqualTo(2));
            Assert.That(bookings[0].BookingStatus, Is.EqualTo(BookingStatus.Confirmed));
            Assert.That(bookings[1].BookingStatus, Is.EqualTo(BookingStatus.Confirmed));
            Assert.That(bookings[2].BookingStatus, Is.EqualTo(BookingStatus.Cancelled));

            // Verify
            _mockBookingRepo.Verify(r => r.Update(It.IsAny<Booking>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that ConfirmPreliminaryBookingsForCustomer returns 0 when faced with invalid CustomerID, without call to Db
        /// </summary>
        [Test]
        [TestCase(0)]
        [TestCase(-5)]
        public void ConfirmPreliminaryBookingsForCustomer_InvalidCustomerId_ReturnsZero(int invalidCustomerId)
        {
            // Act
            int result = _bookingController.ConfirmPreliminaryBookingsForCustomer(invalidCustomerId);

            // Assert
            Assert.That(result, Is.EqualTo(0));

            // Verify
            _mockBookingRepo.Verify(r => r.GetBookingsByCustomer(It.IsAny<int>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }
    }
}