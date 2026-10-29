using BusinessLayer;
using EntityLayer;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class PaymentUnitTests
    {
        private PaymentPlanService _service;

        [SetUp]
        public void Setup()
        {
            _service = new PaymentPlanService();
        }

        #region Less than 30 days to arrival (Entire payment)

        /// <summary>
        /// Verifies that if the arrival date is less than 30 days away, the entire amount is due immediately as one installment.
        /// </summary>
        [Test]
        public void GeneratePaymentPlan_LessThan30Days_GeneratesSingleImmediateInstallment()
        {
            // Arrange
            var booking = new Booking
            {
                BookingDate = new DateTime(2026, 1, 1),
                TotalAmount = 5000m,
                HasCancellationInsurance = false,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = new DateTime(2026, 1, 15) } // 14 days away
                }
            };

            // Act
            var plan = _service.GeneratePaymentPlan(booking);

            // Assert
            Assert.That(plan, Has.Count.EqualTo(1));
            Assert.That(plan[0].InstallmentNumber, Is.EqualTo(1));
            Assert.That(plan[0].Amount, Is.EqualTo(5000m));
            Assert.That(plan[0].DueDate, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(plan[0].Description, Does.Contain("Slutbetalning"));
        }

        #endregion

        #region Over 30 days to arrival (Payment installments)

        /// <summary>
        /// Verifies that bookings more than 30 days away are split 20% / 80% without insurance.
        /// </summary>
        [Test]
        public void GeneratePaymentPlan_MoreThan30DaysNoInsurance_Splits20And80()
        {
            // Arrange
            var booking = new Booking
            {
                BookingDate = new DateTime(2026, 1, 1),
                TotalAmount = 10000m,
                HasCancellationInsurance = false,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = new DateTime(2026, 6, 1) } // Mutliple months away
                }
            };

            // Act
            var plan = _service.GeneratePaymentPlan(booking);

            // Assert
            Assert.That(plan, Has.Count.EqualTo(2));

            // Anmälningsavgift: 20% av 10 000 = 2 000 kr, förfaller om 30 dagar
            Assert.That(plan[0].InstallmentNumber, Is.EqualTo(1));
            Assert.That(plan[0].Amount, Is.EqualTo(2000m));
            Assert.That(plan[0].DueDate, Is.EqualTo(new DateTime(2026, 1, 31)));

            // Slutbetalning: 80% av 10 000 = 8 000 kr, förfaller 30 dagar före ankomst
            Assert.That(plan[1].InstallmentNumber, Is.EqualTo(2));
            Assert.That(plan[1].Amount, Is.EqualTo(8000m));
            Assert.That(plan[1].DueDate, Is.EqualTo(new DateTime(2026, 5, 2)));
        }

        /// <summary>
        /// Verifies that the 300 kr cancellation insurance is fully added to the first installment.
        /// </summary>
        [Test]
        public void GeneratePaymentPlan_MoreThan30DaysWithInsurance_AddsInsuranceToFirstInstallment()
        {
            // Arrange
            var booking = new Booking
            {
                BookingDate = new DateTime(2026, 1, 1),
                TotalAmount = 10300m, // 10 000 kr boende + 300 kr försäkring
                HasCancellationInsurance = true,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = new DateTime(2026, 6, 1) }
                }
            };

            // Act
            var plan = _service.GeneratePaymentPlan(booking);

            // Assert
            Assert.That(plan, Has.Count.EqualTo(2));

            // Anmälningsavgift: 20% av basbeloppet (10 000 = 2 000) + 300 försäkring = 2 300 kr
            Assert.That(plan[0].Amount, Is.EqualTo(2300m));

            // Slutbetalning: 80% av basbeloppet (10 000 = 8 000 kr)
            Assert.That(plan[1].Amount, Is.EqualTo(8000m));
        }

        #endregion

        #region Date calculations

        /// <summary>
        /// Verifies that the earliest date between accommodations and rentals is used as the arrival date.
        /// </summary>
        [Test]
        public void GeneratePaymentPlan_UsesEarliestDateFromAccommodationsOrRentals()
        {
            // Arrange
            var booking = new Booking
            {
                BookingDate = new DateTime(2026, 1, 1),
                TotalAmount = 5000m,
                Accommodations = new List<BookingAccommodation>
                {
                    new BookingAccommodation { StartDate = new DateTime(2026, 6, 15) } // Accommodation starts in june
                },
                Rentals = new List<Rental>
                {
                    new Rental { StartDate = new DateTime(2026, 6, 1) } // Rentals performed early june (Earliest date)
                }
            };

            // Act
            var plan = _service.GeneratePaymentPlan(booking);

            // Assert
            // Final payment is decided by rental.
            Assert.That(plan[1].DueDate, Is.EqualTo(new DateTime(2026, 5, 2)));
        }

        /// <summary>
        /// Verifies fallback behavior when no accommodations or rentals exist.
        /// </summary>
        [Test]
        public void GeneratePaymentPlan_NoDatesFound_FallsBackToBookingDate()
        {
            // Arrange
            var booking = new Booking
            {
                BookingDate = new DateTime(2026, 1, 1),
                TotalAmount = 5000m
            };

            // Act
            var plan = _service.GeneratePaymentPlan(booking);

            // Assert
            // Fallback = BookingDate (Payment made directly)
            Assert.That(plan, Has.Count.EqualTo(1));
            Assert.That(plan[0].DueDate, Is.EqualTo(new DateTime(2026, 1, 1)));
        }

        #endregion
    }
}