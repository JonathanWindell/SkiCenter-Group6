using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class SkiSchoolUnitTests
    {
        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ISkiLessonRepository> _mockSkiLessonRepo;
        private Mock<IBookingRepository> _mockBookingRepo;
        private SkiSchoolController _controller;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSkiLessonRepo = new Mock<ISkiLessonRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();

            _mockUnitOfWork.Setup(u => u.SkiLesson).Returns(_mockSkiLessonRepo.Object);
            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);

            _controller = new SkiSchoolController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Book Participant

        /// <summary>
        /// Verifies that booking with invalid IDs immediately returns false without repository queries.
        /// </summary>
        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        [TestCase(1, 0)]
        [TestCase(1, -5)]
        public void BookParticipant_InvalidIdentifiers_ReturnsFalse(int bookingId, int sessionId)
        {
            // Act
            var (success, message) = _controller.BookParticipant(bookingId, sessionId);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("Ogiltigt"));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that booking returns false if the parent booking record does not exist.
        /// </summary>
        [Test]
        public void BookParticipant_NonExistentBooking_ReturnsFalse()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetByID(It.IsAny<int>())).Returns((Booking)null!);

            // Act
            var (success, _) = _controller.BookParticipant(1, 10);

            // Assert
            Assert.That(success, Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that a booking attempt is rejected when the maximum participant limit has been reached.
        /// </summary>
        [Test]
        public void BookParticipant_SessionIsFull_ReturnsFalse()
        {
            // Arrange
            int bookingId = 100;
            int sessionId = 200;

            // Måste ha ett boende kopplat för att passera kontrollerns validering "kunden är på plats"
            var booking = new Booking
            {
                BookingID = bookingId,
                TotalAmount = 1000m,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(7) }
                }
            };
            _mockBookingRepo.Setup(r => r.GetByID(bookingId)).Returns(booking);

            var lesson = new SkiLesson("Grön", "mån-ons", 10, 400m);
            var session = new SkiLessonSession(1, "Förmiddag", lesson.SkiLessonID) { SkiLessonSessionID = sessionId };
            lesson.Sessions.Add(session);

            _mockSkiLessonRepo.Setup(r => r.GetAll()).Returns(new List<SkiLesson> { lesson });
            _mockSkiLessonRepo.Setup(r => r.GetBookedParticipantCount(sessionId)).Returns(10); // Session is full

            // Act
            var (success, _) = _controller.BookParticipant(bookingId, sessionId);

            // Assert
            Assert.That(success, Is.False, "Should refuse booking when session capacity has reached maximum.");
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that booking an available spot adds the participant, increases TotalAmount, and saves changes.
        /// </summary>
        [Test]
        public void BookParticipant_SpotsAvailable_IncreasesTotalAmountAndReturnsTrue()
        {
            // Arrange
            int bookingId = 101;
            int sessionId = 201;
            decimal initialAmount = 2500m;
            decimal lessonPrice = 455m;

            var booking = new Booking
            {
                BookingID = bookingId,
                TotalAmount = initialAmount,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = new DateTime(2026, 1, 5), EndDate = new DateTime(2026, 1, 11) }
                }
            };
            _mockBookingRepo.Setup(r => r.GetByID(bookingId)).Returns(booking);

            var lesson = new SkiLesson("Svart", "mån-ons", 10, lessonPrice);
            var session = new SkiLessonSession(2, "Eftermiddag", lesson.SkiLessonID) { SkiLessonSessionID = sessionId };
            lesson.Sessions.Add(session);

            _mockSkiLessonRepo.Setup(r => r.GetAll()).Returns(new List<SkiLesson> { lesson });
            _mockSkiLessonRepo.Setup(r => r.GetBookedParticipantCount(sessionId)).Returns(3);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            var (success, message) = _controller.BookParticipant(bookingId, sessionId);

            // Assert
            Assert.That(success, Is.True, $"Misslyckades med fel: {message}");
            Assert.That(booking.TotalAmount, Is.EqualTo(initialAmount + lessonPrice));
            Assert.That(booking.SkiLessons.Any(sl => sl.SkiLessonSessionID == sessionId), Is.True);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Cancel Participant

        /// <summary>
        /// Verifies that canceling a participant deducts the course fee and removes the participant.
        /// </summary>
        [Test]
        public void CancelParticipant_ValidBooking_ReducesTotalAmountAndReturnsTrue()
        {
            // Arrange
            int bookingId = 102;
            int sessionId = 202;
            int lessonBookingId = 505;
            decimal lessonPrice = 500m;

            // Mock repository functionality
            var booking = new Booking { BookingID = bookingId, TotalAmount = 3000m };
            _mockBookingRepo.Setup(r => r.GetByID(bookingId)).Returns(booking);

            // Create ski lesson booking
            var lessonBooking = new SkiLessonBooking
            {
                SkiLessonBookingID = lessonBookingId,
                BookingID = bookingId,
                SkiLessonSessionID = sessionId
            };
            booking.SkiLessons.Add(lessonBooking);

            var lesson = new SkiLesson("Grön", "tor-fre", 10, lessonPrice);
            var session = new SkiLessonSession(3, "Förmiddag", lesson.SkiLessonID) { SkiLessonSessionID = sessionId };
            session.Participants.Add(lessonBooking);
            lesson.Sessions.Add(session);

            _mockSkiLessonRepo.Setup(r => r.GetAll()).Returns(new List<SkiLesson> { lesson });
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _controller.CancelParticipant(lessonBookingId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(booking.TotalAmount, Is.EqualTo(2500m), "The lesson price must be deducted from the booking total.");
            Assert.That(session.Participants.Contains(lessonBooking), Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that attempting to cancel a non-existent participant returns false safely.
        /// </summary>
        [Test]
        public void CancelParticipant_NonExistentParticipant_ReturnsFalse()
        {
            // Arrange
            _mockSkiLessonRepo.Setup(r => r.GetAll()).Returns(new List<SkiLesson>());

            // Act
            bool result = _controller.CancelParticipant(999);

            // Assert
            Assert.That(result, Is.False, "Should safely return false if the participant is not found.");
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        #endregion

        #region Capacity & Availability

        /// <summary>
        /// Verifies that IsSessionAvailable properly checks against the maximum participant threshold.
        /// </summary>
        [TestCase(9, 10, true)]
        [TestCase(10, 10, false)]
        [TestCase(1, 2, true)]
        [TestCase(2, 2, false)]
        public void IsSessionAvailable_ThresholdChecks_ReturnsExpectedResult(int currentBooked, int maxCapacity, bool expectedAvailable)
        {
            // Arrange
            int sessionId = 301;
            var lesson = new SkiLesson("TestNivå", "mån-ons", maxCapacity, 500m);
            var session = new SkiLessonSession(1, "Förmiddag", lesson.SkiLessonID) { SkiLessonSessionID = sessionId };
            lesson.Sessions.Add(session);

            _mockSkiLessonRepo.Setup(r => r.GetAll()).Returns(new List<SkiLesson> { lesson });
            _mockSkiLessonRepo.Setup(r => r.GetBookedParticipantCount(sessionId)).Returns(currentBooked);

            // Act
            bool result = _controller.IsSessionAvailable(sessionId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedAvailable));
        }

        #endregion
    }
}