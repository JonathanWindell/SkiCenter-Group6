using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class PropertyUnitTests
    {
        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IAccommodationRepository> _mockAccommodationRepo;
        private PropertyController _controller;

        [SetUp]
        public void Setup()
        {
            // Create mocks for both Unit of Work and the accommodation repository
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAccommodationRepo = new Mock<IAccommodationRepository>();

            // Configure UnitOfWork to return the mocked accommodation repository
            _mockUnitOfWork.Setup(u => u.Accommodation).Returns(_mockAccommodationRepo.Object);

            // Inject the mocked IUnitOfWork into the controller
            _controller = new PropertyController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Controller Business Logic Tests

        /// <summary>
        /// Test for UpdateAccommodationStatus. Verifies that non-existing or invalid accommodation ID returns false and does not save.
        /// </summary>
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(999)]
        public void UpdateAccommodationStatus_AccommodationNotFound_ReturnsFalse(int accommodationId)
        {
            // Arrange
            _mockAccommodationRepo.Setup(r => r.GetByID(accommodationId)).Returns((Accommodation)null!);

            // Act
            bool result = _controller.UpdateAccommodationStatus(accommodationId, AccommodationStatus.Maintenance);

            // Assert
            Assert.That(result, Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for UpdateAccommodationStatus. Verifies that an existing accommodation gets updated and saved via UnitOfWork
        /// </summary>
        [TestCase(AccommodationStatus.Available)]
        [TestCase(AccommodationStatus.Rented)]
        [TestCase(AccommodationStatus.Damaged)]
        [TestCase(AccommodationStatus.Maintenance)]
        public void UpdateAccommodationStatus_ValidAccommodation_UpdatesStatusAndReturnsTrue(AccommodationStatus newStatus)
        {
            // Arrange
            var accommodation = new Accommodation
            {
                AccommodationID = 10,
                AccommodationNumber = "LI2",
                Status = AccommodationStatus.Available
            };

            _mockAccommodationRepo.Setup(r => r.GetByID(10)).Returns(accommodation);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _controller.UpdateAccommodationStatus(10, newStatus);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(accommodation.Status, Is.EqualTo(newStatus));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Enum Tests (Low Value - Testing .NET Framework)

        /// <summary>
        /// Test for AccommodationStatus enum parsing and underlying values. Verifies that valid string values parse to the expected enum
        /// </summary>
        [TestCase("Available", AccommodationStatus.Available, 0)]
        [TestCase("Rented", AccommodationStatus.Rented, 1)]
        [TestCase("Damaged", AccommodationStatus.Damaged, 2)]
        [TestCase("Maintenance", AccommodationStatus.Maintenance, 3)]
        public void AccommodationStatus_ParseAndUnderlyingValue_MatchesExpected(string statusString, AccommodationStatus expectedStatus, int expectedValue)
        {
            // Act
            bool parseResult = Enum.TryParse<AccommodationStatus>(statusString, out var parsedStatus);

            // Assert
            Assert.That(parseResult, Is.True);
            Assert.That(parsedStatus, Is.EqualTo(expectedStatus));
            Assert.That((int)parsedStatus, Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Test for AccommodationStatus enum parsing with invalid string input
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("UnknownStatus")]
        public void AccommodationStatus_InvalidString_FailsToParse(string invalidStatusString)
        {
            // Act
            bool parseResult = Enum.TryParse<AccommodationStatus>(invalidStatusString, out _);

            // Assert
            Assert.That(parseResult, Is.False);
        }

        #endregion
    }
}