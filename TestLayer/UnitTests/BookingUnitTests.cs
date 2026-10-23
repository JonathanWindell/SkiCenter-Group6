using BusinessLayer;
using DataLayer.Interfaces;
using EntityLayer;
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

        /// <summary>
        /// Verifies that a valid preliminary booking is changed to Cancelled and saved to the database.
        /// </summary>
        [Test]
        public void CancelBooking_ValidPreliminaryBooking_SetsStatusCancelledAndCompletes()
        {
            // Arrange
            int bookingId = 10;
            var booking = new Booking
            {
                BookingID = bookingId,
                BookingStatus = BookingStatus.Preliminary,
                CheckInStatus = CheckInStatus.NotCheckedIn
            };

            _mockBookingRepo.Setup(r => r.GetSpecificBooking(bookingId)).Returns(booking);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            var result = _controller.CancelBooking(bookingId);

            // Assert
            Assert.That(result.BookingStatus, Is.EqualTo(BookingStatus.Cancelled));
            _mockBookingRepo.Verify(r => r.GetSpecificBooking(bookingId), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that CancelBooking throws ArgumentException when booking ID is zero or negative.
        /// </summary>
        [TestCase(0)]
        [TestCase(-1)]
        public void CancelBooking_InvalidId_ThrowsArgumentException(int invalidId)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _controller.CancelBooking(invalidId));
            _mockBookingRepo.Verify(r => r.GetSpecificBooking(It.IsAny<int>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that CancelBooking throws InvalidOperationException if the booking does not exist.
        /// </summary>
        [Test]
        public void CancelBooking_BookingNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            int bookingId = 99;
            _mockBookingRepo.Setup(r => r.GetSpecificBooking(bookingId)).Returns((Booking)null);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _controller.CancelBooking(bookingId));
            Assert.That(ex.Message, Does.Contain("Bokningen kunde inte hittas."));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that attempting to cancel an already cancelled booking throws InvalidOperationException.
        /// </summary>
        [Test]
        public void CancelBooking_AlreadyCancelled_ThrowsInvalidOperationException()
        {
            // Arrange
            int bookingId = 15;
            var booking = new Booking
            {
                BookingID = bookingId,
                BookingStatus = BookingStatus.Cancelled,
                CheckInStatus = CheckInStatus.NotCheckedIn
            };

            _mockBookingRepo.Setup(r => r.GetSpecificBooking(bookingId)).Returns(booking);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _controller.CancelBooking(bookingId));
            Assert.That(ex.Message, Does.Contain("Bokningen är redan avbokad."));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that active/started bookings (e.g., Finished or CheckedIn) cannot be cancelled.
        /// </summary>
        [Test]
        public void CancelBooking_AlreadyCheckedInOrFinished_ThrowsInvalidOperationException()
        {
            // Arrange
            int bookingId = 20;
            var booking = new Booking
            {
                BookingID = bookingId,
                BookingStatus = BookingStatus.Confirmed,
                CheckInStatus = CheckInStatus.CheckedIn
            };

            _mockBookingRepo.Setup(r => r.GetSpecificBooking(bookingId)).Returns(booking);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _controller.CancelBooking(bookingId));
            Assert.That(ex.Message, Does.Contain("En påbörjad eller avslutad bokning kan inte avbokas."));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that without cancellation insurance, the refund is always 0 kr.
        /// </summary>
        [Test]
        public void GetCancellationRefund_NoInsurance_ReturnsZero()
        {
            // Arrange
            var booking = new Booking
            {
                HasCancellationInsurance = false,
                TotalAmount = 5000m
            };

            // Act
            var refund = _controller.GetCancellationRefund(booking, DateTime.Today);

            // Assert
            Assert.That(refund, Is.EqualTo(0m));
        }

        /// <summary>
        /// Verifies that cancelling at least 5 days prior to arrival refunds 80% of (TotalAmount - 300 kr).
        /// Example: Total 5300 kr - 300 kr fee = 5000 kr * 0.80 = 4000 kr.
        /// </summary>
        [Test]
        [TestCase("2026-12-10", "2026-12-20", 5300, 4000)] // 10 days before
        [TestCase("2026-12-15", "2026-12-20", 5300, 4000)] // Exactly 5 days before (boundary condition)
        [TestCase("2026-12-01", "2026-12-10", 2300, 1600)] // (2300 - 300) * 0.80 = 1600
        public void GetCancellationRefund_AtLeast5DaysBefore_ReturnsCalculatedRefund(
            string cancelDateStr, string arrivalDateStr, decimal totalAmount, decimal expectedRefund)
        {
            // Arrange
            DateTime cancelDate = DateTime.Parse(cancelDateStr);
            DateTime arrivalDate = DateTime.Parse(arrivalDateStr);

            var booking = new Booking
            {
                HasCancellationInsurance = true,
                TotalAmount = totalAmount,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = arrivalDate }
                }
            };

            // Act
            var refund = _controller.GetCancellationRefund(booking, cancelDate);

            // Assert
            Assert.That(refund, Is.EqualTo(expectedRefund));
        }

        /// <summary>
        /// Verifies that cancelling closer than 5 days prior to arrival returns null (no refund granted).
        /// </summary>
        [Test]
        [TestCase("2026-12-16", "2026-12-20")] // 4 days before
        [TestCase("2026-12-19", "2026-12-20")] // 1 day before
        [TestCase("2026-12-20", "2026-12-20")] // Day of arrival
        public void GetCancellationRefund_LessThan5DaysBefore_ReturnsNull(string cancelDateStr, string arrivalDateStr)
        {
            // Arrange
            DateTime cancelDate = DateTime.Parse(cancelDateStr);
            DateTime arrivalDate = DateTime.Parse(arrivalDateStr);

            var booking = new Booking
            {
                HasCancellationInsurance = true,
                TotalAmount = 5000m,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = arrivalDate }
                }
            };

            // Act
            var refund = _controller.GetCancellationRefund(booking, cancelDate);

            // Assert
            Assert.That(refund, Is.Null, "Expected refund to be null when cancelling less than 5 days before arrival.");
        }

        /// <summary>
        /// Verifies that GetCancellationRefund throws ArgumentNullException when booking is null.
        /// </summary>
        [Test]
        public void GetCancellationRefund_NullBooking_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _controller.GetCancellationRefund(null, DateTime.Today));
        }

        /// <summary>
        /// Verifies that GetCancellationRefund throws InvalidOperationException if TotalAmount is less than the 300 kr protection fee.
        /// </summary>
        [Test]
        public void GetCancellationRefund_TotalAmountLowerThanFee_ThrowsInvalidOperationException()
        {
            // Arrange
            var booking = new Booking
            {
                HasCancellationInsurance = true,
                TotalAmount = 200m, // Below 300 kr fee
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = DateTime.Today.AddDays(10) }
                }
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _controller.GetCancellationRefund(booking, DateTime.Today));
            Assert.That(ex.Message, Does.Contain("Bokningsbeloppet är lägre än avgiften"));
        }
    }
}
