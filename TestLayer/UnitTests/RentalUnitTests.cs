using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class RentalUnitTests
    {
        private const int BookingId = 10;

        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBookingRepository> _mockBookingRepo;
        private Mock<IEquipmentItemRepository> _mockEquipmentItemRepo;
        private Mock<IRentalRepository> _mockRentalRepo;
        private SkiShopController _controller;
        private Booking _trackedBooking;

        [SetUp]
        public void Setup()
        {
            // Create mocks for Unit of Work and the repositories used when renting out equipment
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockEquipmentItemRepo = new Mock<IEquipmentItemRepository>();
            _mockRentalRepo = new Mock<IRentalRepository>();

            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.EquipmentItem).Returns(_mockEquipmentItemRepo.Object);
            _mockUnitOfWork.Setup(u => u.Rental).Returns(_mockRentalRepo.Object);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // An active booking with a room stay from today and six days ahead
            Booking activeBooking = CreateBooking(DateTime.Today, DateTime.Today.AddDays(6));
            _mockBookingRepo.Setup(r => r.FindActiveBookings(BookingId, null)).Returns(new[] { activeBooking });

            _trackedBooking = new Booking { BookingID = BookingId, TotalAmount = 5000m };
            _mockBookingRepo.Setup(r => r.GetByID(BookingId)).Returns(_trackedBooking);

            _controller = new SkiShopController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Helper Methods (Test Data Builders)

        private static Booking CreateBooking(DateTime stayStart, DateTime stayEnd)
        {
            var booking = new Booking { BookingID = BookingId, BookingStatus = BookingStatus.Confirmed };
            booking.Accommodations.Add(new BookingAccommodation { StartDate = stayStart, EndDate = stayEnd });
            return booking;
        }

        /// <summary>
        /// Creates an item. A rented item also gets an active rental from today and six days ahead,
        /// since availability is checked against rental dates and not only the status.
        /// </summary>
        private static EquipmentItem CreateItem(int id, EquipmentStatus status, string category = "Alpint")
        {
            EquipmentItem item = CreateItemWithoutRentals(id, status, category);

            if (status == EquipmentStatus.Rented)
            {
                AddRental(item, DateTime.Today, DateTime.Today.AddDays(6));
            }

            return item;
        }

        private static void AddRental(EquipmentItem item, DateTime start, DateTime end)
        {
            var rental = new Rental { StartDate = start, EndDate = end, Status = "Active" };
            item.RentalItems.Add(new RentalItem { Rental = rental, EquipmentItem = item });
        }

        private static EquipmentItem CreateItemWithoutRentals(int id, EquipmentStatus status, string category)
        {
            return new EquipmentItem
            {
                EquipmentItemID = id,
                ArticleNumber = $"ART{id}",
                Status = status,
                Equipment = new Equipment("Testutrustning", category)
                {
                    Prices = new List<EquipmentPriceMatrix>
                    {
                        new EquipmentPriceMatrix { Days = 1, TotalAmount = 100 },
                        new EquipmentPriceMatrix { Days = 3, TotalAmount = 250 },
                        new EquipmentPriceMatrix { Days = 5, TotalAmount = 400 }
                    }
                }
            };
        }

        private void SetupItems(params EquipmentItem[] items)
        {
            _mockEquipmentItemRepo
                .Setup(r => r.GetItemsByIds(It.IsAny<IEnumerable<int>>()))
                .Returns(items);
        }

        #endregion

        #region Availability & Validation Tests

        /// <summary>
        /// Verifies that equipment that is already rented, damaged or on maintenance cannot be added to a booking.
        /// </summary>
        [Test]
        [TestCase(EquipmentStatus.Rented)]
        [TestCase(EquipmentStatus.Damaged)]
        [TestCase(EquipmentStatus.Maintenance)]
        public void AddEquipmentToBooking_UnavailableItem_ReturnsFalse(EquipmentStatus status)
        {
            // Arrange
            EquipmentItem item = CreateItem(1, status);
            SetupItems(item);

            // Act
            var (success, _, addedAmount) = _controller.AddEquipmentToBooking(BookingId, new[] { 1 }, DateTime.Today, DateTime.Today, false);

            // Assert
            Assert.That(success, Is.False, $"Equipment with status {status} should not be possible to rent out.");
            Assert.That(addedAmount, Is.EqualTo(0m));
            Assert.That(item.Status, Is.EqualTo(status), "The item's status must not be changed.");
            Assert.That(_trackedBooking.TotalAmount, Is.EqualTo(5000m), "The booking total must not be changed.");

            // Verify
            _mockRentalRepo.Verify(r => r.Add(It.IsAny<Rental>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that equipment rented out in another period can still be booked for free dates.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_ItemRentedInOtherPeriod_CanBeRented()
        {
            // Arrange: the item is rented out next month, the booking's stay is now
            EquipmentItem item = CreateItemWithoutRentals(1, EquipmentStatus.Available, "Alpint");
            AddRental(item, DateTime.Today.AddMonths(1), DateTime.Today.AddMonths(1).AddDays(3));
            SetupItems(item);

            // Act
            var (success, message, _) = _controller.AddEquipmentToBooking(BookingId, new[] { 1 }, DateTime.Today, DateTime.Today, false);

            // Assert
            Assert.That(success, Is.True, message);
        }

        /// <summary>
        /// Verifies that one unavailable item stops the whole rental, so no items are rented out.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_OneOfSeveralItemsRented_NothingIsRentedOut()
        {
            // Arrange
            EquipmentItem available = CreateItem(1, EquipmentStatus.Available);
            EquipmentItem rented = CreateItem(2, EquipmentStatus.Rented);
            SetupItems(available, rented);

            // Act
            var (success, _, _) = _controller.AddEquipmentToBooking(BookingId, new[] { 1, 2 }, DateTime.Today, DateTime.Today, false);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(available.Status, Is.EqualTo(EquipmentStatus.Available), "The available item must stay available.");

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that the rental period must be within the room stay and not in the past.
        /// </summary>
        [Test]
        [TestCase(0, 7)]   // Ends after the stay
        [TestCase(-1, 2)]  // Starts before today
        [TestCase(3, 2)]   // Ends before it starts
        public void AddEquipmentToBooking_InvalidPeriod_ReturnsFalse(int startOffset, int endOffset)
        {
            // Arrange
            SetupItems(CreateItem(1, EquipmentStatus.Available));

            // Act
            var (success, _, _) = _controller.AddEquipmentToBooking(
                BookingId, new[] { 1 }, DateTime.Today.AddDays(startOffset), DateTime.Today.AddDays(endOffset), false);

            // Assert
            Assert.That(success, Is.False);

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        #endregion

        #region Domain Rules & Core Rental Logic

        /// <summary>
        /// Verifies that equipment cannot be added to a booking that is not an active room booking.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_BookingNotActive_ReturnsFalse()
        {
            // Arrange
            SetupItems(CreateItem(1, EquipmentStatus.Available));
            _mockBookingRepo.Setup(r => r.FindActiveBookings(BookingId, null)).Returns(Enumerable.Empty<Booking>());

            // Act
            var (success, _, _) = _controller.AddEquipmentToBooking(BookingId, new[] { 1 }, DateTime.Today, DateTime.Today, false);

            // Assert
            Assert.That(success, Is.False);

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that a snowmobile cannot be rented out without a verified driver's license.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_SnowmobileWithoutLicense_ReturnsFalse()
        {
            // Arrange
            EquipmentItem snowmobile = CreateItem(1, EquipmentStatus.Available, SkiShopController.SnowmobileCategory);
            SetupItems(snowmobile);

            // Act
            var (success, _, _) = _controller.AddEquipmentToBooking(BookingId, new[] { 1 }, DateTime.Today, DateTime.Today, false);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(snowmobile.Status, Is.EqualTo(EquipmentStatus.Available));

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that a snowmobile can be rented out when the driver's license is verified.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_SnowmobileWithLicense_RentsSnowmobile()
        {
            // Arrange
            EquipmentItem snowmobile = CreateItem(1, EquipmentStatus.Available, SkiShopController.SnowmobileCategory);
            SetupItems(snowmobile);

            Rental? addedRental = null;
            _mockRentalRepo.Setup(r => r.Add(It.IsAny<Rental>())).Callback<Rental>(rental => addedRental = rental);

            // Act
            var (success, message, _) = _controller.AddEquipmentToBooking(BookingId, new[] { 1 }, DateTime.Today, DateTime.Today, true);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(snowmobile.Status, Is.EqualTo(EquipmentStatus.Rented));
            Assert.That(addedRental!.LicenseVerified, Is.True);
        }

        /// <summary>
        /// Verifies that available equipment is linked to the booking, changes status to Rented
        /// and that the price is added to the booking's total amount.
        /// </summary>
        [Test]
        public void AddEquipmentToBooking_AvailableItems_RentsItemsAndAddsPriceToBooking()
        {
            // Arrange
            EquipmentItem first = CreateItem(1, EquipmentStatus.Available);
            EquipmentItem second = CreateItem(2, EquipmentStatus.Available);
            SetupItems(first, second);

            Rental? addedRental = null;
            _mockRentalRepo.Setup(r => r.Add(It.IsAny<Rental>())).Callback<Rental>(rental => addedRental = rental);

            // Act: three days, which costs 250 per item
            var (success, message, addedAmount) = _controller.AddEquipmentToBooking(
                BookingId, new[] { 1, 2 }, DateTime.Today, DateTime.Today.AddDays(2), false);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(addedAmount, Is.EqualTo(500m));
            Assert.That(first.Status, Is.EqualTo(EquipmentStatus.Rented));
            Assert.That(second.Status, Is.EqualTo(EquipmentStatus.Rented));
            Assert.That(_trackedBooking.TotalAmount, Is.EqualTo(5500m));

            Assert.That(addedRental, Is.Not.Null);
            Assert.That(addedRental!.BookingID, Is.EqualTo(BookingId));
            Assert.That(addedRental.TotalPrice, Is.EqualTo(500m));
            Assert.That(addedRental.RentalItems.Select(ri => ri.EquipmentItem), Is.EquivalentTo(new[] { first, second }));
            Assert.That(addedRental.Status, Is.EqualTo("Active"));

            // Verify
            _mockRentalRepo.Verify(r => r.Add(It.IsAny<Rental>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Price Calculation Algorithm

        /// <summary>
        /// Verifies the rental price for different lengths, including rentals longer than the price list.
        /// </summary>
        [Test]
        [TestCase(1, 100)]  // Exact match
        [TestCase(2, 250)]  // Uses the 3-day price
        [TestCase(5, 400)]  // Exact match
        [TestCase(7, 650)]  // 5 days + 2 days (3-day price)
        [TestCase(11, 900)] // 5 + 5 + 1 days
        public void CalculateRentalPrice_DifferentLengths_ReturnsExpectedPrice(int days, int expectedPrice)
        {
            // Arrange
            var prices = CreateItem(1, EquipmentStatus.Available).Equipment.Prices;

            // Act
            decimal? price = SkiShopController.CalculateRentalPrice(prices, days);

            // Assert
            Assert.That(price, Is.EqualTo((decimal)expectedPrice));
        }

        #endregion
    }
}