using DataLayer;
using EntityLayer;
using PresentationLayer;

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
            var existingStaff = Staff.firstName == "Johan";
            existingStaff = Staff.password == "CorrectBookingAdminPassword";

            // Assert
            Assert.That(existingStaff, Is.True, "System should find existing staff and allow login");

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
            var existingStaff = Staff.firstName == "Johan";
            existingStaff = Staff.password == "WrongBookingAdminPassword";

            // Assert
            Assert.That(existingStaff, Is.False, "System should find and deny login");
        }

        /// <summary>
        /// Error path for testcase IB-ES-01.1
        /// </summary>
        [Test]
        public void SkiShop_DenyAccessToView()
        {
            //Arrange
            var Staff = new Staff("SkiShop", "Vincent", "Adolfsson", "SkiShop@test.se", "TestSkiShopPassword");

            // Act
            var existingStaff = Staff.firstName == "Johan";
            existingStaff = Staff.password == "WrongBookingAdminPassword";



            // Assert
            Assert.IsFalse(existingStaff, "System should find and deny login");
        }
    }
}
