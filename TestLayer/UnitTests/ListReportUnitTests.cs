using BusinessLayer.Controllers;
using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;
using ServiceLayer.Pdf;
using System.Text;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class ListReportUnitTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IStaffRepository> _mockStaffRepo;
        private ListReportController _controller;
        private LoginController _loginController;
        private PdfReportService _pdfService;

        [SetUp]
        public void Setup()
        {
            // Create mocks for Unit of Work and every repository the reports read from
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStaffRepo = new Mock<IStaffRepository>();

            var customerRepo = new Mock<ICustomerRepository>();
            var equipmentItemRepo = new Mock<IEquipmentItemRepository>();
            var equipmentRepo = new Mock<IEquipmentRepository>();
            var seasonPriceRepo = new Mock<ISeasonPriceRepository>();
            var skiLessonRepo = new Mock<ISkiLessonRepository>();

            _mockUnitOfWork.Setup(u => u.Staff).Returns(_mockStaffRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(customerRepo.Object);
            _mockUnitOfWork.Setup(u => u.EquipmentItem).Returns(equipmentItemRepo.Object);
            _mockUnitOfWork.Setup(u => u.Equipment).Returns(equipmentRepo.Object);
            _mockUnitOfWork.Setup(u => u.SeasonPrice).Returns(seasonPriceRepo.Object);
            _mockUnitOfWork.Setup(u => u.SkiLesson).Returns(skiLessonRepo.Object);

            customerRepo.Setup(r => r.GetAllCustomers()).Returns(CreateCustomers());
            equipmentItemRepo.Setup(r => r.GetAllEquipmentItems()).Returns(CreateEquipmentItems());
            equipmentRepo.Setup(r => r.GetAllEquipmentWithPrices()).Returns(new[]
            {
                new Equipment("Skidor", "Alpint")
                {
                    Prices = new List<EquipmentPriceMatrix>
                    {
                        new EquipmentPriceMatrix { Days = 1, TotalAmount = 130 },
                        new EquipmentPriceMatrix { Days = 3, TotalAmount = 330 }
                    }
                }
            });
            seasonPriceRepo
                .Setup(r => r.GetCurrentPrices(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>()))
                .Returns(CreateSeasonPrices());
            skiLessonRepo.Setup(r => r.GetLessonsWithSessions()).Returns(new[] { new SkiLesson("Grön", "mån-ons", 10, 400m) });

            _controller = new ListReportController(_mockUnitOfWork.Object);
            _loginController = new LoginController(_mockUnitOfWork.Object);
            _pdfService = new PdfReportService();
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
        /// Two private customers (one with a booking) and two corporate customers (one not yet approved).
        /// </summary>
        private static List<Customer> CreateCustomers()
        {
            var withBooking = new PrivateCustomer("Anna", "Andersson", "Storgatan 1", "anna@test.se", "0701111111", 12000m);
            withBooking.Bookings.Add(new Booking());

            return new List<Customer>
            {
                withBooking,
                new PrivateCustomer("Erik", "Eriksson", "Lillgatan 2", "erik@test.se", "0702222222", 12000m),
                new CorporateCustomer("Fjällbolaget AB", "5561234567", "Lisa Larsson", "Bergsvägen 3", "info@fjall.se", "0703333333") { IsApproved = true, DiscountRate = 0.10m },
                new CorporateCustomer("Nytt AB", "5569876543", "Olle Olsson", "Dalvägen 4", "info@nytt.se", "0704444444")
            };
        }

        /// <summary>
        /// Alpint: 2 available, 1 rented, 1 damaged. Skoter: 1 on maintenance.
        /// </summary>
        private static List<EquipmentItem> CreateEquipmentItems()
        {
            var alpint = new Equipment("Skidor", "Alpint");
            var skoter = new Equipment("Lynx 50", "Skoter");

            return new List<EquipmentItem>
            {
                new EquipmentItem { ArticleNumber = "A1", Size = "170", Status = EquipmentStatus.Available, Equipment = alpint },
                new EquipmentItem { ArticleNumber = "A2", Size = "160", Status = EquipmentStatus.Available, Equipment = alpint },
                new EquipmentItem { ArticleNumber = "A3", Size = "150", Status = EquipmentStatus.Rented, Equipment = alpint },
                new EquipmentItem { ArticleNumber = "A4", Size = "140", Status = EquipmentStatus.Damaged, Equipment = alpint },
                new EquipmentItem { ArticleNumber = "S1", Size = "-", Status = EquipmentStatus.Maintenance, Equipment = skoter }
            };
        }

        /// <summary>
        /// Weekly prices for one apartment type: week 2 and week 50 cost 3 000 kr, week 8 is high season at 4 000 kr.
        /// </summary>
        private static List<SeasonPrice> CreateSeasonPrices()
        {
            var type = new AccommodationType { AccommodationTypeID = 1, CategoryCode = "LGH.I", Description = "Lägenhet 50 kvm" };

            return new List<SeasonPrice>
            {
                new SeasonPrice(1, "vecka", 2, 3000m, DateTime.Today, null) { AccommodationType = type },
                new SeasonPrice(1, "vecka", 8, 4000m, DateTime.Today, null) { AccommodationType = type },
                new SeasonPrice(1, "vecka", 50, 3000m, DateTime.Today, null) { AccommodationType = type },
                new SeasonPrice(1, "dygn", 8, 700m, DateTime.Today, null) { AccommodationType = type }
            };
        }

        /// <summary>
        /// Verifies that staff without the MarketingManager or SystemAdmin role cannot create reports.
        /// </summary>
        [Test]
        [TestCase(staffRole.SkiShop)]
        [TestCase(staffRole.BookingAdmin)]
        public void CreateReports_NotAllowedRole_ReturnsFalse(staffRole role)
        {
            // Arrange
            LogInAs(role);

            // Act + Assert
            Assert.That(_controller.CreateCustomerRegister().success, Is.False);
            Assert.That(_controller.CreateEquipmentRegister().success, Is.False);
            Assert.That(_controller.CreatePriceList().success, Is.False);
            Assert.That(_controller.CreateStatisticsReport(new StatisticsReport(), false).success, Is.False);
        }

        /// <summary>
        /// Verifies the customer register's counts and key messages.
        /// </summary>
        [Test]
        public void CreateCustomerRegister_Customers_CountsTypesAndPendingApprovals()
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);

            // Act
            var (success, message, report) = _controller.CreateCustomerRegister();

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(report!.Customers, Has.Count.EqualTo(4));
            Assert.That(report.PrivateCustomerCount, Is.EqualTo(2));
            Assert.That(report.CorporateCustomerCount, Is.EqualTo(2));
            Assert.That(report.PendingCorporateCount, Is.EqualTo(1));
            Assert.That(report.KeyMessages, Has.Some.Contains("1 företagskund(er) väntar på godkännande"));
            Assert.That(report.KeyMessages, Has.Some.Contains("3 kund(er) har ännu inte gjort någon bokning"));

            CustomerRegisterRow corporate = report.Customers.Single(c => c.Name == "Fjällbolaget AB");
            Assert.That(corporate.OrganisationNumber, Is.EqualTo("5561234567"));
            Assert.That(corporate.DiscountRate, Is.EqualTo(0.10m));
        }

        /// <summary>
        /// Verifies the status summary per category and that damaged and maintenance items need action.
        /// </summary>
        [Test]
        public void CreateEquipmentRegister_Items_SummarizesStatusPerCategory()
        {
            // Arrange
            LogInAs(staffRole.SystemAdmin);

            // Act
            var (success, message, report) = _controller.CreateEquipmentRegister();

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(report!.TotalItems, Is.EqualTo(5));
            Assert.That(report.AvailableItems, Is.EqualTo(2));
            Assert.That(report.RentedItems, Is.EqualTo(1));
            Assert.That(report.NeedsActionItems, Is.EqualTo(2));
            Assert.That(report.AvailableShare, Is.EqualTo(0.4m));

            EquipmentCategorySummary alpint = report.Categories.Single(c => c.Category == "Alpint");
            Assert.That(alpint.Available, Is.EqualTo(2));
            Assert.That(alpint.Rented, Is.EqualTo(1));
            Assert.That(alpint.Damaged, Is.EqualTo(1));
            Assert.That(alpint.Total, Is.EqualTo(4));
            Assert.That(report.KeyMessages, Has.Some.Contains("2 artikel(ar) är skadade eller på service"));
        }

        /// <summary>
        /// Verifies that the price list finds the lowest price, the high season weeks and orders weeks by season.
        /// </summary>
        [Test]
        public void BuildAccommodationPriceLists_WeeklyPrices_FindsHighSeasonAndOrdersBySeason()
        {
            // Act
            AccommodationPriceList list = ListReportController.BuildAccommodationPriceLists(CreateSeasonPrices()).Single();

            // Assert
            Assert.That(list.LowestWeekPrice, Is.EqualTo(3000m));
            Assert.That(list.HighestWeekPrice, Is.EqualTo(4000m));
            Assert.That(list.HighSeasonWeeks, Is.EqualTo(new[] { 8 }));
            Assert.That(list.PriceTypes, Is.EqualTo(new[] { "vecka", "dygn" }));
            Assert.That(list.Weeks.Select(w => w.WeekNumber), Is.EqualTo(new[] { 50, 2, 8 }), "The season runs over new year.");
            Assert.That(list.Weeks.Single(w => w.WeekNumber == 8).Prices["dygn"], Is.EqualTo(700m));
        }

        /// <summary>
        /// Verifies that every report is generated as a real PDF file.
        /// </summary>
        [Test]
        public void PdfReportService_AllReports_GeneratesPdfFiles()
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);

            var statistics = new StatisticsReport
            {
                FromMonth = new DateTime(2026, 10, 1),
                ToMonth = new DateTime(2026, 10, 1),
                CurrentPeriod = new List<MonthlyStatistic> { new MonthlyStatistic { Month = new DateTime(2026, 10, 1), AvailableNights = 310, BookedNights = 31, AccommodationSales = 5000m } },
                PreviousPeriod = new List<MonthlyStatistic> { new MonthlyStatistic { Month = new DateTime(2025, 10, 1), AvailableNights = 310 } }
            };
            statistics.CurrentTotals = PeriodSummary.FromMonths(statistics.CurrentPeriod);
            statistics.PreviousTotals = PeriodSummary.FromMonths(statistics.PreviousPeriod);

            // Act
            var pdfs = new[]
            {
                _pdfService.CreateStatisticsPdf(_controller.CreateStatisticsReport(statistics, true).report!),
                _pdfService.CreateCustomerRegisterPdf(_controller.CreateCustomerRegister().report!),
                _pdfService.CreateEquipmentRegisterPdf(_controller.CreateEquipmentRegister().report!),
                _pdfService.CreatePriceListPdf(_controller.CreatePriceList().report!)
            };

            // Assert: every PDF file starts with "%PDF"
            foreach (byte[] pdf in pdfs)
            {
                Assert.That(pdf.Length, Is.GreaterThan(1000));
                Assert.That(Encoding.ASCII.GetString(pdf, 0, 4), Is.EqualTo("%PDF"));
            }
        }
    }
}
