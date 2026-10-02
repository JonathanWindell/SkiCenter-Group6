using DataLayer;

namespace TestLayer.IntegrationTests
{
    public class BookingIntegrationTests
    {
        [TestFixture]
        public class BookingTests
        {
            private SkiCenterDbContext _context;

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

        }
    }
}
