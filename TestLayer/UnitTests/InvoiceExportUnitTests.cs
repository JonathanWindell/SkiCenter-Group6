using BusinessLayer;
using BusinessLayer.Controllers;
using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;
using System.Text.Json;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class InvoiceExportUnitTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBookingRepository> _mockBookingRepo;
        private Mock<IStaffRepository> _mockStaffRepo;
        private ReportController _controller;
        private LoginController _loginController;

        [SetUp]
        public void Setup()
        {
            // Create mocks for Unit of Work and the repositories used by the controllers
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockStaffRepo = new Mock<IStaffRepository>();

            _mockUnitOfWork.Setup(u => u.Booking).Returns(_mockBookingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Staff).Returns(_mockStaffRepo.Object);

            _mockBookingRepo
                .Setup(r => r.GetFinishedBookingsForInvoicing(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new[] { CreateCorporateBooking(), CreatePrivateBooking() });

            _controller = new ReportController(_mockUnitOfWork.Object);
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
        /// Corporate booking: accommodation 5 600 kr with 10% discount, cancellation insurance,
        /// one rented item (3 days, 250 kr) and one ski lesson (1 060 kr). Total 6 650 kr.
        /// </summary>
        private static Booking CreateCorporateBooking()
        {
            var booking = new Booking
            {
                BookingID = 1,
                BookingStatus = BookingStatus.Finished,
                HasCancellationInsurance = true,
                AccommodationListPrice = 5600m,
                DiscountRate = 0.10m,
                DiscountAmount = 560m,
                TotalAmount = 6650m,
                Customer = new CorporateCustomer("Fjällbolaget AB", "5561234567", "Anna Andersson", "Storgatan 1", "info@fjallbolaget.se", "0701234567")
                {
                    CustomerID = 100
                }
            };

            booking.Accommodations.Add(new BookingAccommodation
            {
                StartDate = new DateTime(2026, 10, 12),
                EndDate = new DateTime(2026, 10, 15),
                Accommodation = new Accommodation("LI1", AccommodationStatus.Available)
                {
                    AccommodationType = new AccommodationType { CategoryCode = "LGH.I" }
                }
            });

            var item = new EquipmentItem
            {
                ArticleNumber = "AS1",
                Size = "170",
                Equipment = new Equipment("Skidor", "Alpint")
                {
                    Prices = new List<EquipmentPriceMatrix>
                    {
                        new EquipmentPriceMatrix { Days = 1, TotalAmount = 100 },
                        new EquipmentPriceMatrix { Days = 3, TotalAmount = 250 }
                    }
                }
            };

            var rental = new Rental
            {
                RentalID = 5,
                StartDate = new DateTime(2026, 10, 12),
                EndDate = new DateTime(2026, 10, 14),
                TotalPrice = 250m
            };
            rental.EquipmentItems.Add(item);
            booking.Rentals.Add(rental);

            booking.SkiLessons.Add(new SkiLessonBooking
            {
                SkiLessonSession = new SkiLessonSession(42, "10:00-12:00", 1)
                {
                    SkiLesson = new SkiLesson("Nybörjare", "Mån-Fre", 10, 1060m) { SkiLessonID = 1 }
                }
            });

            return booking;
        }

        /// <summary>
        /// Private booking created before discounts were saved: only an accommodation, total 4 480 kr.
        /// </summary>
        private static Booking CreatePrivateBooking()
        {
            var booking = new Booking
            {
                BookingID = 2,
                BookingStatus = BookingStatus.Finished,
                TotalAmount = 4480m,
                Customer = new PrivateCustomer("Lisa", "Larsson", "Fjällvägen 2", "lisa@test.se", "0709876543", 12000m)
                {
                    CustomerID = 200
                }
            };

            booking.Accommodations.Add(new BookingAccommodation
            {
                StartDate = new DateTime(2026, 10, 10),
                EndDate = new DateTime(2026, 10, 13),
                Accommodation = new Accommodation("LII1", AccommodationStatus.Available)
                {
                    AccommodationType = new AccommodationType { CategoryCode = "LGH.II" }
                }
            });

            return booking;
        }

        /// <summary>
        /// Verifies that staff without the MarketingManager or SystemAdmin role cannot generate exports.
        /// </summary>
        [Test]
        [TestCase(staffRole.SkiShop)]
        [TestCase(staffRole.BookingAdmin)]
        public void GenerateInvoiceExport_NotAllowedRole_ReturnsFalse(staffRole role)
        {
            // Arrange
            LogInAs(role);

            // Act
            var (success, _, export) = _controller.GenerateInvoiceExport(DateTime.Today.AddDays(-30), DateTime.Today);

            // Assert
            Assert.That(success, Is.False, $"{role} should not be allowed to export invoicing data.");
            Assert.That(export, Is.Null);

            // Verify
            _mockBookingRepo.Verify(r => r.GetFinishedBookingsForInvoicing(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        /// <summary>
        /// Verifies that no export can be generated when no one is logged in.
        /// </summary>
        [Test]
        public void GenerateInvoiceExport_NotLoggedIn_ReturnsFalse()
        {
            // Act
            var (success, _, _) = _controller.GenerateInvoiceExport(DateTime.Today.AddDays(-30), DateTime.Today);

            // Assert
            Assert.That(success, Is.False);
        }

        /// <summary>
        /// Verifies that marketing managers and system admins can generate exports for completed bookings.
        /// </summary>
        [Test]
        [TestCase(staffRole.MarketingManager)]
        [TestCase(staffRole.SystemAdmin)]
        public void GenerateInvoiceExport_AllowedRole_ReturnsAllCompletedBookings(staffRole role)
        {
            // Arrange
            LogInAs(role);

            // Act
            var (success, message, export) = _controller.GenerateInvoiceExport(DateTime.Today.AddDays(-30), DateTime.Today);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(export!.BookingCount, Is.EqualTo(2));
            Assert.That(export.TotalInclVat, Is.EqualTo(6650m + 4480m));
        }

        /// <summary>
        /// Verifies that an end date before the start date is rejected.
        /// </summary>
        [Test]
        public void GenerateInvoiceExport_EndBeforeStart_ReturnsFalse()
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);

            // Act
            var (success, _, _) = _controller.GenerateInvoiceExport(DateTime.Today, DateTime.Today.AddDays(-1));

            // Assert
            Assert.That(success, Is.False);
        }

        /// <summary>
        /// Verifies that the customer filter only includes the selected customer type.
        /// </summary>
        [Test]
        [TestCase(InvoiceCustomerFilter.Corporate, 1)]
        [TestCase(InvoiceCustomerFilter.Private, 2)]
        public void GenerateInvoiceExport_CustomerFilter_ReturnsOnlyThatCustomerType(InvoiceCustomerFilter filter, int expectedBookingId)
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);

            // Act
            var (_, _, export) = _controller.GenerateInvoiceExport(DateTime.Today.AddDays(-30), DateTime.Today, filter);

            // Assert
            Assert.That(export!.Bookings.Select(b => b.BookingID), Is.EqualTo(new[] { expectedBookingId }));
        }

        /// <summary>
        /// Verifies that every article becomes its own line with the correct VAT rate and VAT amount.
        /// </summary>
        [Test]
        public void BuildBookingInvoice_CorporateBooking_ListsEveryArticleWithCorrectVat()
        {
            // Act
            BookingInvoice invoice = ReportController.BuildBookingInvoice(CreateCorporateBooking());

            // Assert
            Assert.That(invoice.Lines, Has.Count.EqualTo(4));

            InvoiceLine accommodation = invoice.Lines.Single(l => l.ArticleType == "Boende");
            Assert.That(accommodation.ListPriceInclVat, Is.EqualTo(5600m));
            Assert.That(accommodation.DiscountInclVat, Is.EqualTo(560m));
            Assert.That(accommodation.AmountInclVat, Is.EqualTo(5040m));
            Assert.That(accommodation.VatRate, Is.EqualTo(VatRates.Accommodation));
            Assert.That(accommodation.AmountExclVat, Is.EqualTo(4500m));
            Assert.That(accommodation.VatAmount, Is.EqualTo(540m));

            InvoiceLine equipment = invoice.Lines.Single(l => l.ArticleType == "Utrustning");
            Assert.That(equipment.ArticleNumber, Is.EqualTo("AS1"));
            Assert.That(equipment.AmountExclVat, Is.EqualTo(200m));
            Assert.That(equipment.VatAmount, Is.EqualTo(50m));

            InvoiceLine lesson = invoice.Lines.Single(l => l.ArticleType == "Skidskola");
            Assert.That(lesson.AmountExclVat, Is.EqualTo(1000m));
            Assert.That(lesson.VatAmount, Is.EqualTo(60m));

            InvoiceLine insurance = invoice.Lines.Single(l => l.ArticleType == "Avbeställningsskydd");
            Assert.That(insurance.VatAmount, Is.EqualTo(0m));
        }

        /// <summary>
        /// Verifies that the applied discount is listed and that VAT is summarized per rate for corporate customers.
        /// </summary>
        [Test]
        public void BuildBookingInvoice_CorporateBooking_ListsDiscountAndSeparatesVatPerRate()
        {
            // Act
            BookingInvoice invoice = ReportController.BuildBookingInvoice(CreateCorporateBooking());

            // Assert
            Assert.That(invoice.Customer.CustomerType, Is.EqualTo("Företagskund"));
            Assert.That(invoice.Customer.OrganisationNumber, Is.EqualTo("5561234567"));

            AppliedDiscount discount = invoice.Discounts.Single();
            Assert.That(discount.Description, Is.EqualTo("Företagsrabatt"));
            Assert.That(discount.Rate, Is.EqualTo(0.10m));
            Assert.That(discount.AmountInclVat, Is.EqualTo(560m));

            Assert.That(invoice.VatSummary.Select(v => v.VatRate), Is.EquivalentTo(new[] { 0.25m, 0.12m, 0.06m, 0m }));
            Assert.That(invoice.TotalExclVat, Is.EqualTo(6000m));
            Assert.That(invoice.TotalVat, Is.EqualTo(650m));
            Assert.That(invoice.TotalInclVat, Is.EqualTo(6650m));
            Assert.That(invoice.MatchesBookingTotal, Is.True);
        }

        /// <summary>
        /// Verifies that bookings created before discounts were saved still get an accommodation line from the booking total.
        /// </summary>
        [Test]
        public void BuildBookingInvoice_BookingWithoutSavedListPrice_UsesBookingTotal()
        {
            // Act
            BookingInvoice invoice = ReportController.BuildBookingInvoice(CreatePrivateBooking());

            // Assert
            InvoiceLine accommodation = invoice.Lines.Single();
            Assert.That(accommodation.AmountInclVat, Is.EqualTo(4480m));
            Assert.That(accommodation.AmountExclVat, Is.EqualTo(4000m));
            Assert.That(invoice.Discounts, Is.Empty);
            Assert.That(invoice.MatchesBookingTotal, Is.True);
        }

        /// <summary>
        /// Verifies that the JSON export can be read back and contains the lines, discounts and VAT.
        /// </summary>
        [Test]
        public void ToJson_Export_ContainsLinesDiscountsAndVat()
        {
            // Arrange
            LogInAs(staffRole.MarketingManager);
            var (_, _, export) = _controller.GenerateInvoiceExport(DateTime.Today.AddDays(-30), DateTime.Today);

            // Act
            string json = ReportController.ToJson(export!);

            // Assert
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement firstBooking = document.RootElement.GetProperty("bookings")[0];

            Assert.That(document.RootElement.GetProperty("bookingCount").GetInt32(), Is.EqualTo(2));
            Assert.That(firstBooking.GetProperty("lines").GetArrayLength(), Is.EqualTo(4));
            Assert.That(firstBooking.GetProperty("discounts")[0].GetProperty("description").GetString(), Is.EqualTo("Företagsrabatt"));
            Assert.That(firstBooking.GetProperty("vatSummary").GetArrayLength(), Is.EqualTo(4));
            Assert.That(json, Does.Contain("Företagskund"), "Swedish characters should not be escaped.");
        }
    }
}
