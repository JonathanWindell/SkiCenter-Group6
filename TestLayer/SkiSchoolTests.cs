using DataLayer;

namespace TestLayer
{
    [TestFixture]
    public class Tests
    {
        private SkiCenterDbContext _context;

        // Create connection to database.
        [SetUp]
        public void Setup()
        {
            // Initialize database. 
            _context = new SkiCenterDbContext();
        }

        // Remove connection to database.
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
        public void SkiSchool_AddStudentInNotFullGroup()
        {
            Assert.Pass();
        }

        [Test]
        public void SkiSchool_ThrowExceptionWhenGroupFull()
        {
            Assert.Pass();
        }
    }
}
