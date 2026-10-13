using BusinessLayer;
using DataLayer;

namespace TestLayer.IntegrationTests
{
    public class RentalIntegrationTests
    {
        private SkiCenterDbContext _context;
        private UnitOfWork unitOfWork;

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

        //Integrate Businesslayer
        [Test]
        public void SkiRental_BookAvailableSkis()
        {
            // Arrange

            // Act

            // Assert

        }


        //Integrate Businesslayer
        [Test]
        public void SkiRental_BookDamagedSkis()
        {
            // Arrange

            // Act

            // Assert

        }

        //Integrate Businesslayer
        [Test]
        public void AccomodationRental_BookAvailableApartment()
        {
            // Arrange

            // Act

            // Assert

        }

        //Integrate Businesslayer
        [Test]
        public void AccomodationRental_BookOccupiedApartment()
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
    }
}
