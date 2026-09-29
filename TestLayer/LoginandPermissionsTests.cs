using DataLayer;
using EntityLayer;

/*
namespace TestLayer
{
    [TestFixture]
    public class CustomerRegistryTests
    {
        private SkiCenterDbContext _context;

        /// <summary>
        /// Happy path for testcase IB-HP-01.1
        /// </summary>
        [Test]
        public void BookingAdminExists_SuccesfulLogin()
        {
            //Arrange
            var Staff = new Staff("BookingAdmin", "Johan", "Windell", "BookingAdmin@test.se", "TestBookingAdminPassword");

            // Act
            string inputEmail = "BookingAdmin@test.se";
            string inputPassword = "CorrectBookingAdminPassword";

            bool loginSuccess = (Staff.Email == inputEmail && Staff.PasswordHash == inputPassword);

            // Assert
            Assert.That(loginSuccess, Is.True, "System should find existing staff and allow login");

        }

        /// <summary>
        /// Error path for testcase IB-EP-01.2
        /// </summary>
        [Test]
        public void BookingAdminExists_IncorrectLoginInformation()
        {
            //Arrange
            var Staff = new Staff("BookingAdmin", "Johan", "Windell", "BookingAdmin@test.se", "TestBookingAdminPassword");

            // Act
            string inputEmail = "Johan";
            string inputPassword = "WrongBookingAdminPassword";

            bool loginSuccess = (Staff.Email == inputEmail && Staff.PasswordHash == inputPassword);

            // Assert
            Assert.That(loginSuccess, Is.False, "System should find and deny login");
        }

        /// <summary>
        /// Error path for testcase IB-ES-01.1
        /// </summary>
        [Test]
        public void SkiShop_DenyAccessToView()
        {
            //Arrange
            var loggedInUser = new Staff("Vincent", "SkiShop@Test.se", "TestSkiShopPassword", staffRole.SkiShop);

            // Act
            bool hasAccesstoSeasonPrices = CheckAccessToSeasonPrices(loggedInUser.Role);

            // Assert
            Assert.That(hasAccessToSeasonPrices, Is.False, "Ski-shop personal should be denied season price view");
        }

        // Should be placed in business layer later,
        private bool CheckAccessToSeasonPrices(StaffRole currentRole)
        {
            if (currentRole == StaffRole.SystemAdmin)
            {
                return true;
            }
            return false; 
        }
    }
}
*/