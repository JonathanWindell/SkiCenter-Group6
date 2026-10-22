using BusinessLayer.Controllers;
using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class StatisticsUnitTests
    {
        private const int ApartmentTypeId = 1;
        private const int CampingTypeId = 3;

        private static readonly DateTime October = new DateTime(2026, 10, 1);
        private static readonly DateTime November = new DateTime(2026, 11, 1);

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBookingRepository> _mockBookingRepo;
        private Mock<IAccommodationRepository> _mockAccommodationRepo;
        private Mock<IStaffRepository> _mockStaffRepo;
        private StatisticsController _controller;
        private LoginController _loginController;

        [SetUp]
        public void Setup()
        {
            // Create mocks for Unit of Work and the repositories used by the controllers
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockAccommodationRepo = new Mock<IAccommodationRepository>();
            _mockStaffRepo = new Mock<IStaffRepository>();

            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Accommodation).Returns(_mockAccommodationRepo.Object);
            _mockUnitOfWork.Setup(u => u.Staff).Returns(_mockStaffRepo.Object);

            // Ten apartments and five camping sites
            var apartmentType = new AccommodationType { AccommodationTypeID = ApartmentTypeId, CategoryCode = "LGH.I", Description = "Lägenhet 50 kvm" };
            var campingType = new AccommodationType { AccommodationTypeID = CampingTypeId, CategoryCode = "Camp", Description = "Campingplats" };

            var accommodations = Enumerable.Range(1, 10)
                .Select(i => new Accommodation($"LI{i}", AccommodationStatus.Available) { AccommodationTypeID = ApartmentTypeId, AccommodationType = apartmentType })
                .Concat(Enumerable.Range(1, 5)
                    .Select(i => new Accommodation($"C{i}", AccommodationStatus.Available) { AccommodationTypeID = CampingTypeId, AccommodationType = campingType }))
                .ToList();

            _mockAccommodationRepo.Setup(r => r.GetAllAccommodations()).Returns(accommodations);
            _mockBookingRepo
                .Setup(r => r.GetBookingsForStatistics(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Enumerable.Empty<Booking>());

            _controller = new StatisticsController(_mockUnitOfWork.Object);
            _loginController = new LoginController(_mockUnitOfWork.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // The logged in user is static, so reset it between tests
            _loginController.Logout();
        }

        private void LogInAs(staffRole role)
        {
            var staff = new Staff("Test", "Person", "test@skicenter.se", "HashedPassword123", role) { StaffID = 7 };

            _mockStaffRepo
                .Setup(r => r.GetStaffByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(staff);

            _loginController.Login(staff.Email, "AnyPassword123");
        }

        /// <summary>
        /// A booking with a stay from 28 October to 2 November: 4 nights in October and 1 in November.
        /// Accommodation 5 600 kr incl. VAT, which is 5 000 kr excl. 12% VAT.
        /// </summary>
        private static Booking CreateBooking(int typeId = ApartmentTypeId, BookingStatus status = BookingStatus.Confirmed)
        {
            var booking = new Booking
            {
                BookingID = 1,
                BookingStatus = status,
                AccommodationListPrice = 5600m,
                TotalAmount = 5600m,
                Customer = new PrivateCustomer("Lisa", "Larsson", "Fjällvägen 2", "lisa@test.se", "0709876543", 12000m)
            };

            booking.Accommodations.Add(new BookingAccommodation
            {
                StartDate = new DateTime(2026, 10, 28),
                EndDate = new DateTime(2026, 11, 2),
                Accommodation = new Accommodation("LI1", AccommodationStatus.Available) { AccommodationTypeID = typeId }
            });

            return booking;
        }

        private static List<MonthlyStatistic> Calculate(IEnumerable<Booking> bookings, int units = 10, int? typeId = null)
        {
            return StatisticsController.CalculateMonthlyStatistics(bookings, new[] { October, November }, units, typeId);
        }

        /// <summary>
        /// Verifies that a stay over a month boundary is split into the right number of nights per month.
        /// </summary>
        [Test]
        public void CalculateMonthlyStatistics_StayOverMonthBoundary_SplitsNightsPerMonth()
        {
            // Act
            List<MonthlyStatistic> result = Calculate(new[] { CreateBooking() });

            // Assert
            Assert.That(result[0].BookedNights, Is.EqualTo(4), "28–31 October should be 4 nights.");
            Assert.That(result[1].BookedNights, Is.EqualTo(1), "1 November should be 1 night.");
            Assert.That(result[0].AvailableNights, Is.EqualTo(10 * 31));
            Assert.That(result[1].AvailableNights, Is.EqualTo(10 * 30));
            Assert.That(result[0].OccupancyRate, Is.EqualTo(Math.Round(4m / 310m, 4)));
        }

        /// <summary>
        /// Verifies that accommodation sales excl. VAT are spread over the nights.
        /// </summary>
        [Test]
        public void CalculateMonthlyStatistics_AccommodationSales_SpreadOverNights()
        {
            // Act
            List<MonthlyStatistic> result = Calculate(new[] { CreateBooking() });

            // Assert
            Assert.That(result[0].AccommodationSales, Is.EqualTo(4000m));
            Assert.That(result[1].AccommodationSales, Is.EqualTo(1000m));
            Assert.That(result.Sum(m => m.TotalSales), Is.EqualTo(5000m));
        }

        /// <summary>
        /// Verifies that a booking counts in the month of check-in only.
        /// </summary>
        [Test]
        public void CalculateMonthlyStatistics_Booking_CountsInCheckInMonth()
        {
            // Act
            List<MonthlyStatistic> result = Calculate(new[] { CreateBooking() });

            // Assert
            Assert.That(result[0].BookingCount, Is.EqualTo(1));
            Assert.That(result[1].BookingCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that cancelled bookings are not counted, while preliminary and finished bookings are.
        /// </summary>
        [Test]
        [TestCase(BookingStatus.Cancelled, 0)]
        [TestCase(BookingStatus.Preliminary, 4)]
        [TestCase(BookingStatus.Finished, 4)]
        public void CalculateMonthlyStatistics_BookingStatus_OnlyCancelledIsExcluded(BookingStatus status, int expectedOctoberNights)
        {
            // Act
            List<MonthlyStatistic> result = Calculate(new[] { CreateBooking(status: status) });

            // Assert
            Assert.That(result[0].BookedNights, Is.EqualTo(expectedOctoberNights));
        }

        /// <summary>
        /// Verifies that the accommodation type filter excludes bookings of other types.
        /// </summary>
        [Test]
        public void CalculateMonthlyStatistics_TypeFilter_ExcludesOtherTypes()
        {
            // Arrange
            var bookings = new[] { CreateBooking(ApartmentTypeId), CreateBooking(CampingTypeId) };

            // Act
            List<MonthlyStatistic> result = Calculate(bookings, units: 5, typeId: CampingTypeId);

            // Assert
            Assert.That(result[0].BookedNights, Is.EqualTo(4), "Only the camping booking should be counted.");
            Assert.That(result[0].BookingCount, Is.EqualTo(1));
            Assert.That(result[0].AvailableNights, Is.EqualTo(5 * 31));
        }

        /// <summary>
        /// Verifies that equipment sales are counted in the month the equipment was rented.
        /// </summary>
        [Test]
        public void CalculateMonthlyStatistics_Rental_CountedInRentalMonth()
        {
            // Arrange: one day of rental, 250 kr incl. VAT = 200 kr excl. 25% VAT
            Booking booking = CreateBooking();
            booking.TotalAmount += 250m;

            var rental = new Rental 
            { 
                StartDate = DateTime.Now, 
                EndDate = DateTime.Now, 
                TotalPrice = 250m,
                RentalItems = new List<RentalItem>()
            };

            var testEquipmentItem = new EquipmentItem
            {
                ArticleNumber = "AS1",
                Equipment = new Equipment("Skidor", "Alpint")
                {
                    Prices = new List<EquipmentPriceMatrix>
                    {
                        new EquipmentPriceMatrix { Days = 1, TotalAmount = 250 }
                    }
                }
            };

            rental.RentalItems.Add(new RentalItem
            {
                EquipmentItem = testEquipmentItem
            });

            booking.Rentals.Add(rental);

            // Act
            List<MonthlyStatistic> result = Calculate(new[] { booking });

            // Assert
            Assert.That(result[0].EquipmentSales, Is.EqualTo(0m));
            Assert.That(result[1].EquipmentSales, Is.EqualTo(200m));
        }

        /// <summary>
        /// Verifies that staff without the MarketingManager or SystemAdmin role cannot view statistics.
        /// </summary>
        [Test]
        [TestCase(staffRole.SkiShop)]
        [TestCase(staffRole.BookingAdmin)]
        public void GetStatistics_NotAllowedRole_ReturnsFalse(staffRole role)
        {
            // Arrange
            LogInAs(role);

            // Act
            var (success, _, report) = _controller.GetStatistics(October, November);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(report, Is.Null);

            // Verify
            _mockBookingRepo.Verify(r => r.GetBookingsForStatistics(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Verifies that marketing managers and system admins get statistics for the period and the previous year.
        /// </summary>
        [Test]
        [TestCase(staffRole.MarketingManager)]
        [TestCase(staffRole.SystemAdmin)]
        public void GetStatistics_AllowedRole_ReturnsCurrentAndPreviousYear(staffRole role)
        {
            // Arrange
            LogInAs(role);

            // Act
            var (success, message, report) = _controller.GetStatistics(October, November);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(report!.CurrentPeriod.Select(m => m.Month), Is.EqualTo(new[] { October, November }));
            Assert.That(report.PreviousPeriod.Select(m => m.Month), Is.EqualTo(new[] { October.AddYears(-1), November.AddYears(-1) }));

            // Verify both periods are fetched
            _mockBookingRepo.Verify(r => r.GetBookingsForStatistics(October, new DateTime(2026, 12, 1)), Times.Once);
            _mockBookingRepo.Verify(r => r.GetBookingsForStatistics(October.AddYears(-1), new DateTime(2025, 12, 1)), Times.Once);
        }

        /// <summary>
        /// Verifies that invalid periods are rejected.
        /// </summary>
        [Test]
        public void GetStatistics_InvalidPeriod_ReturnsFalse()
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);

            // Act
            var (endBeforeStart, _, _) = _controller.GetStatistics(November, October);
            var (tooLong, _, _) = _controller.GetStatistics(October, October.AddMonths(StatisticsController.MaxMonths));

            // Assert
            Assert.That(endBeforeStart, Is.False);
            Assert.That(tooLong, Is.False);
        }

        /// <summary>
        /// Verifies the change against the previous period, and that no change is shown without previous data.
        /// </summary>
        [Test]
        [TestCase(150, 100, 0.5)]
        [TestCase(50, 100, -0.5)]
        public void PercentChange_WithPreviousData_ReturnsChange(int current, int previous, double expected)
        {
            Assert.That(StatisticsReport.PercentChange(current, previous), Is.EqualTo((decimal)expected));
        }

        [Test]
        public void PercentChange_NoPreviousData_ReturnsNull()
        {
            Assert.That(StatisticsReport.PercentChange(100, 0), Is.Null);
        }
    }
}
