using DataLayer;
using EntityLayer;
using BusinessLayer.Controllers;

namespace TestLayer
{
    [TestFixture]
    public class LoginAndPermissionTests
    {
        private SkiCenterDbContext _context;

        /// <summary>
        /// Happy path for testcase IB-HP-01.1
        /// </summary>
        [Test]
        public void BookingAdminExists_SuccesfulLogin()
        {
            //Arrange
            var Staff = new Staff("Johan", "Windell", "BookingAdmin@test.se", "TestBookingAdminPassword", staffRole.BookingAdmin);

            // Act
            string inputEmail = "BookingAdmin@test.se";
            string inputPassword = "CorrectBookingAdminPassword";

            bool loginSuccess = (Staff.Email == inputEmail && Staff.Password == inputPassword);

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
            var Staff = new Staff("Johan", "Windell", "BookingAdmin@test.se", "TestBookingAdminPassword", staffRole.BookingAdmin);

            // Act
            string inputEmail = "Johan";
            string inputPassword = "WrongBookingAdminPassword";

            bool loginSuccess = (Staff.Email == inputEmail && Staff.Password == inputPassword);

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
            var loggedInUser = new Staff("Vincent", "SkiShop@Test.se", "TestSkiShopPassword", "TestSkiShopPassword", staffRole.SkiShop);

            // Act
            bool hasAccesstoSeasonPrices = StaffController.CheckAccessToSeasonPrices(loggedInUser.Role);

            // Assert
            Assert.That(hasAccesstoSeasonPrices, Is.False, "Ski-shop personal should be denied season price view");
        }
    }
}
