using BusinessLayer;
using DataLayer;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integraton")]
    public class PaymentIntegrationTests
    {
        private SkiCenterDbContext _context;

        private UnitOfWork _unitOfWork;

        private BookingController _bookingController;

        [SetUp]
        public void Setup()
        {
            // Initialize database. 
            _context = new SkiCenterDbContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure the DbContext is disposed after each test to avoid resource leaks
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        [Test]
        public void CustomerBooking_GeneratePaymentPlan()
        {
            // Arrange
            /*
             * Create temp booking with todays date, arrival date (75 days ahead), total sum
             */

            // Act
            /*
             * Save booking to database
             * Fetch BusinessLayer logic regarding payment plans
             */

            // Assert
            /*
             * Payment plan is generated correctly based on payment plan rules. 
             */

            /*
            // Remove test booking
            */

        }

        [Test]
        public void CustomerBooking_InstantPayment()
        {
            // Arrange
            /*
             * Create temp booking with todays date, arrival date (15 days ahead), total sum
             */

            // Act
            /*
             * Save booking to database
             * Fetch BusinessLayer logic regarding payment plans
             */

            // Assert
            /*
             * Payment plan is generated correctly based on payment plan rules. Eg entire payment should be payed directly.
             */

            /*
            // Remove test booking
            */
        }

    }
}
