using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class SeasonPriceUnitTests
    {
        private const int AccommodationTypeId = 1;
        private const string PriceType = "vecka";
        private const int Week = 8;

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ISeasonPriceRepository> _mockSeasonPriceRepo;
        private Mock<IStaffRepository> _mockStaffRepo;
        private AdminController _controller;
        private LoginController _loginController;

        [SetUp]
        public void Setup()
        {
            // Create mocks for Unit of Work and the repositories used by the controllers
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSeasonPriceRepo = new Mock<ISeasonPriceRepository>();
            _mockStaffRepo = new Mock<IStaffRepository>();

            _mockUnitOfWork.Setup(u => u.SeasonPrice).Returns(_mockSeasonPriceRepo.Object);
            _mockUnitOfWork.Setup(u => u.Staff).Returns(_mockStaffRepo.Object);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            _controller = new AdminController(_mockUnitOfWork.Object);
            _loginController = new LoginController(_mockUnitOfWork.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // The logged in user is static, so reset it between tests
            _loginController.Logout();
        }

        /// <summary>
        /// Logs in a staff member with the given role through the LoginController using the mocked staff repository.
        /// </summary>
        private Staff LogInAs(staffRole role)
        {
            var staff = new Staff("Test", "Person", "test@skicenter.se", "HashedPassword123", role) { StaffID = 7 };

            _mockStaffRepo
                .Setup(r => r.GetStaffByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(staff);

            _loginController.Login(staff.Email, "AnyPassword123");
            return staff;
        }

        private SeasonPrice CreateExistingPrice()
        {
            var existing = new SeasonPrice(AccommodationTypeId, PriceType, Week, 4500m, new DateTime(2025, 12, 1), 1)
            {
                SeasonPriceID = 1
            };

            _mockSeasonPriceRepo
                .Setup(r => r.GetSeasonPrice(AccommodationTypeId, PriceType, Week))
                .Returns(existing);

            return existing;
        }

        /// <summary>
        /// Verifies that saving a new price adds a new row and never overwrites the historical price.
        /// </summary>
        [Test]
        public void ChangeSeasonPrice_ValidNewPrice_AddsNewPriceAndKeepsHistoricalPrice()
        {
            // Arrange
            Staff admin = LogInAs(staffRole.SystemAdmin);
            SeasonPrice existing = CreateExistingPrice();
            DateTime originalValidFrom = existing.ValidFrom;

            SeasonPrice? addedPrice = null;
            _mockSeasonPriceRepo
                .Setup(r => r.Add(It.IsAny<SeasonPrice>()))
                .Callback<SeasonPrice>(price => addedPrice = price);

            // Act
            var (success, message) = _controller.ChangeSeasonPrice(AccommodationTypeId, PriceType, Week, 5000m);

            // Assert
            Assert.That(success, Is.True, message);

            Assert.That(existing.Price, Is.EqualTo(4500m), "The historical price must not be changed.");
            Assert.That(existing.ValidFrom, Is.EqualTo(originalValidFrom), "The historical date must not be changed.");

            Assert.That(addedPrice, Is.Not.Null, "A new price row should be added.");
            Assert.That(addedPrice!.Price, Is.EqualTo(5000m));
            Assert.That(addedPrice.AccommodationTypeID, Is.EqualTo(AccommodationTypeId));
            Assert.That(addedPrice.PriceType, Is.EqualTo(PriceType));
            Assert.That(addedPrice.WeekNumber, Is.EqualTo(Week));
            Assert.That(addedPrice.ValidFrom, Is.GreaterThan(originalValidFrom));
            Assert.That(addedPrice.ChangedByStaffID, Is.EqualTo(admin.StaffID));
            Assert.That(addedPrice, Is.Not.SameAs(existing));

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Once);
            _mockSeasonPriceRepo.Verify(r => r.Update(It.IsAny<SeasonPrice>()), Times.Never);
            _mockSeasonPriceRepo.Verify(r => r.UpdatePrice(It.IsAny<SeasonPrice>()), Times.Never);
            _mockSeasonPriceRepo.Verify(r => r.Delete(It.IsAny<SeasonPrice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that negative and zero prices are rejected and nothing is saved.
        /// </summary>
        [Test]
        [TestCase(-100)]
        [TestCase(0)]
        public void ChangeSeasonPrice_NegativeOrZeroPrice_ReturnsFalse(decimal invalidPrice)
        {
            // Arrange
            LogInAs(staffRole.SystemAdmin);
            CreateExistingPrice();

            // Act
            var (success, _) = _controller.ChangeSeasonPrice(AccommodationTypeId, PriceType, Week, invalidPrice);

            // Assert
            Assert.That(success, Is.False, "Expected negative or zero price to be rejected.");

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that staff without the SystemAdmin role cannot change season prices.
        /// </summary>
        [Test]
        [TestCase(staffRole.SkiShop)]
        [TestCase(staffRole.BookingAdmin)]
        [TestCase(staffRole.MarketingManager)]
        public void ChangeSeasonPrice_NotSystemAdmin_ReturnsFalse(staffRole role)
        {
            // Arrange
            LogInAs(role);
            CreateExistingPrice();

            // Act
            var (success, _) = _controller.ChangeSeasonPrice(AccommodationTypeId, PriceType, Week, 5000m);

            // Assert
            Assert.That(success, Is.False, $"{role} should not be allowed to change season prices.");

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that no price can be changed when no one is logged in.
        /// </summary>
        [Test]
        public void ChangeSeasonPrice_NotLoggedIn_ReturnsFalse()
        {
            // Arrange
            CreateExistingPrice();

            // Act
            var (success, _) = _controller.ChangeSeasonPrice(AccommodationTypeId, PriceType, Week, 5000m);

            // Assert
            Assert.That(success, Is.False);

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Never);
        }

        /// <summary>
        /// Verifies that a price cannot be registered for a combination that has no existing price.
        /// </summary>
        [Test]
        public void ChangeSeasonPrice_UnknownTypeAndWeek_ReturnsFalse()
        {
            // Arrange
            LogInAs(staffRole.SystemAdmin);

            // Act
            var (success, _) = _controller.ChangeSeasonPrice(99, PriceType, Week, 5000m);

            // Assert
            Assert.That(success, Is.False);

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Never);
        }

        /// <summary>
        /// Verifies that invalid accommodation types, empty price types and invalid week numbers
        /// are rejected before the database is used.
        /// </summary>
        [Test]
        [TestCase(0, "vecka", 8)]
        [TestCase(1, "", 8)]
        [TestCase(1, "   ", 8)]
        [TestCase(1, "vecka", 0)]
        [TestCase(1, "vecka", 54)]
        public void ChangeSeasonPrice_InvalidInput_ReturnsFalse(int accommodationTypeId, string priceType, int week)
        {
            // Arrange
            LogInAs(staffRole.SystemAdmin);

            // Act
            var (success, _) = _controller.ChangeSeasonPrice(accommodationTypeId, priceType, week, 5000m);

            // Assert
            Assert.That(success, Is.False);

            // Verify
            _mockSeasonPriceRepo.Verify(r => r.GetSeasonPrice(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _mockSeasonPriceRepo.Verify(r => r.Add(It.IsAny<SeasonPrice>()), Times.Never);
        }
    }
}
