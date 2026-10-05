using BusinessLayer;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;
using NUnit.Framework;

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

            // Verify that the database repository was NEVER queried
            _mockBookingRepo.Verify(r => r.GetBookingsInRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Verifies that when 
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
            _mockBookingRepo.Verify(r => r.GetBookingsInRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /*
        /// <summary>
        /// Verifies that when 
        /// </summary>
        [Test]
        public void GetBookingsInRange_ValidData_ReturnsTrue()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(7);

            // Refactor since booking does not have start and end date
            var fakeBookings = new List<Booking>
            {
                new Booking { BookingID = 1, BookingDate =  },
                new Booking { BookingID = 2, StartDate = startDate.AddDays(1), EndDate = endDate }
            };
         

            // Instruct the mock to return fakeBookings when queried with these dates
            _mockBookingRepo.Setup(r => r.GetBookingsInRange(startDate, endDate))
                            .Returns(fakeBookings);

            // Act
            var result = _controller.GetBookingsInRange(startDate, endDate);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2), "Expected to receive all bookings supplied by the repository.");

            // Verify that the repository was actually called exactly once with the right arguments
            _mockBookingRepo.Verify(r => r.GetBookingsInRange(startDate, endDate), Times.Once);
        }
        */
    }
}
