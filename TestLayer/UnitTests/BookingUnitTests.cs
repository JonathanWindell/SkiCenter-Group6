using BusinessLayer;
using DataLayer.Interfaces;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class BookingUnitTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBookingRepository> _mockBookingRepo;
        private BookingController _controller;

        [SetUp]
        public void Setup()
        {
            // Create mocks for both Unit of Work and the specific repository
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBookingRepo = new Mock<IBookingRepository>();

            // Configure UnitOfWork to return the mocked booking repository
            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);

            // Inject the mocked IUnitOfWork into the controller
            _controller = new BookingController(_mockUnitOfWork.Object);
        }


        /// <summary>
        /// Verifies that when End date is before Start date database an empty list is returned
        /// </summary>
        [Test]
        [TestCase("2026-10-10", "2026-10-05")] // End date before start date
        [TestCase("2026-10-10", "2026-10-10")] // Start date equals end date
        public void GetBookingsInRange_EmptyFields_ReturnsEmpty(string startStr, string endStr)
        {
            // Arrange
            DateTime startDate = DateTime.Parse(startStr);
            DateTime endDate = DateTime.Parse(endStr);

            // Act
            var result = _controller.GetBookingsInRange(startDate, endDate);

            // Assert
            Assert.That(result, Is.Empty, "Expected empty collection when dates are logically invalid.");

            // Verify 
            _mockBookingRepo.Verify(r => r.GetBookingsInRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Verifies that when start date is invalid result is empty. 
        /// </summary>
        [Test]
        public void GetBookingsInRange_NonValidData_ReturnsTrue()
        {
            // Arrange
            DateTime invalidStart = DateTime.MinValue;
            DateTime validEnd = DateTime.Today.AddDays(2);

            // Act
            var result = _controller.GetBookingsInRange(invalidStart, validEnd);

            // Assert
            Assert.That(result, Is.Empty);

            //Verify
            _mockBookingRepo.Verify(r => r.GetBookingsInRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Verifies that when CustomerID is not valid and empty list is returned. 
        /// </summary>
        [Test]
        public void GetBookingsByCustomer_InvalidCustomerId_ReturnsEmptyCollection()
        {
            // Arrange 
            int invalidCustomerID = -2;

            // Act
            var result = _controller.GetBookingsByCustomer(invalidCustomerID);

            // Assert
            Assert.That(result, Is.Empty, "Expected an empty collection for an invalid CustomerID.");

            // Verify
            _mockBookingRepo.Verify(r => r.GetBookingsByCustomer(It.IsAny<int>()), Times.Never);
        }
    }
}
