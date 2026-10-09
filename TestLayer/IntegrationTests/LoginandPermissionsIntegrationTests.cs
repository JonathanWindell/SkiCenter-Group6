using BusinessLayer;
using BusinessLayer.Controllers;
using DataLayer;
using EntityLayer;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class LoginAndPermissionTests
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

        /// <summary>
        /// Verifies that when user enters correct login data the user is denied access. 
        /// </summary>
        [Test]
        public void BookingAdminExists_ValidData_ReturnsTrue()
        {
            //Arrange
            string plainPassword = "TestBookingAdminPassword";
            string hashedPassword = HashingService.HashPassword(plainPassword);

            var staff = new Staff("Johan", "Windell", "admin@test.se", hashedPassword, staffRole.BookingAdmin);

            // Act
            string inputEmail = "admin@test.se";
            string inputPasswordHashed = HashingService.HashPassword("TestBookingAdminPassword");

            bool loginSuccess = (staff.Email == inputEmail && staff.Password == inputPasswordHashed);

            // Assert
            Assert.That(loginSuccess, Is.True, "System should find existing staff and allow login");

        }

        /// <summary>
        /// Verifies that when user enters incorrect login data the user is denied access. 
        /// </summary>
        [Test]
        public void BookingAdminExists_NonValidData_ReturnsFalse()
        {
            //Arrange
            var Staff = new Staff("Johan", "Windell", "BookingAdmin@test.se", "TestBookingAdminPassword", staffRole.BookingAdmin);

            // Act
            string inputEmail = "Johan";
            string inputPassword = "WrongBookingAdminPassword";

            bool loginSuccess = (Staff.Email == inputEmail && Staff.Password == inputPassword);

            // Assert
            Assert.That(loginSuccess, Is.False, "System should find and deny login");
        }

        /// <summary>
        /// Verifies that Ski-shop employee is denied access to season prices view.
        /// </summary>
        [Test]
        public void SkiShop_DenyAccessToView()
        {
            //Arrange
            var loggedInUser = new Staff("Vincent", "SkiShop@Test.se", "TestSkiShopPassword", "TestSkiShopPassword", staffRole.SkiShop);

            // Act
            bool hasAccesstoSeasonPrices = StaffController.CanChangeSeasonPrices(loggedInUser.Role);

            // Assert
            Assert.That(hasAccesstoSeasonPrices, Is.False, "Ski-shop personal should be denied season price view");
        }
    }
}
