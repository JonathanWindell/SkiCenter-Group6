using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class StaffAccountUnitTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IStaffRepository> _mockStaffRepo;
        private StaffController _controller;
        private LoginController _loginController;

        private Staff _admin;
        private Staff _skiShopEmployee;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStaffRepo = new Mock<IStaffRepository>();

            _mockUnitOfWork.Setup(u => u.Staff).Returns(_mockStaffRepo.Object);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            _admin = new Staff("Ada", "Admin", "admin@skicenter.se", "HashedPassword123", staffRole.SystemAdmin) { StaffID = 1 };
            _skiShopEmployee = new Staff("Sam", "Skidbutik", "sam@skicenter.se", "HashedPassword123", staffRole.SkiShop) { StaffID = 2 };

            SetupStaff(_admin, _skiShopEmployee);

            _controller = new StaffController(_mockUnitOfWork.Object);
            _loginController = new LoginController(_mockUnitOfWork.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // The logged in user is static, so reset it between tests
            _loginController.Logout();
        }

        private void SetupStaff(params Staff[] staff)
        {
            _mockStaffRepo.Setup(r => r.GetAllStaff()).Returns(staff);

            foreach (Staff member in staff)
            {
                _mockStaffRepo.Setup(r => r.GetByID(member.StaffID)).Returns(member);
            }
        }

        private void LogInAs(Staff staff)
        {
            _mockStaffRepo
                .Setup(r => r.GetStaffByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(staff);

            _loginController.Login(staff.Email, "AnyPassword123");
        }

        /// <summary>
        /// Verifies that a system admin can deactivate another staff member's account.
        /// </summary>
        [Test]
        public void SetStaffActive_AdminDeactivatesEmployee_AccountIsDeactivated()
        {
            // Arrange
            LogInAs(_admin);

            // Act
            var (success, message) = _controller.SetStaffActive(_skiShopEmployee.StaffID, false);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(_skiShopEmployee.IsActive, Is.False);

            // Verify
            _mockStaffRepo.Verify(r => r.Update(_skiShopEmployee), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Verifies that a deactivated account can be reactivated, e.g. when someone returns from leave.
        /// </summary>
        [Test]
        public void SetStaffActive_AdminReactivatesEmployee_AccountIsActiveAgain()
        {
            // Arrange
            _skiShopEmployee.IsActive = false;
            LogInAs(_admin);

            // Act
            var (success, message) = _controller.SetStaffActive(_skiShopEmployee.StaffID, true);

            // Assert
            Assert.That(success, Is.True, message);
            Assert.That(_skiShopEmployee.IsActive, Is.True);
        }

        /// <summary>
        /// Verifies that admins cannot deactivate their own account and lock themselves out.
        /// </summary>
        [Test]
        public void SetStaffActive_AdminDeactivatesSelf_ReturnsFalse()
        {
            // Arrange
            var otherAdmin = new Staff("Bo", "Admin", "bo@skicenter.se", "HashedPassword123", staffRole.SystemAdmin) { StaffID = 3 };
            SetupStaff(_admin, _skiShopEmployee, otherAdmin);
            LogInAs(_admin);

            // Act
            var (success, _) = _controller.SetStaffActive(_admin.StaffID, false);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(_admin.IsActive, Is.True);

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that the last active system admin cannot be deactivated.
        /// </summary>
        [Test]
        public void SetStaffActive_LastActiveAdmin_ReturnsFalse()
        {
            // Arrange: two admins, the other one is already deactivated
            var lastAdmin = new Staff("Bo", "Admin", "bo@skicenter.se", "HashedPassword123", staffRole.SystemAdmin) { StaffID = 3 };
            _admin.IsActive = false;
            SetupStaff(_admin, _skiShopEmployee, lastAdmin);

            // Logged in as an active admin (not in the list, so it is not "self")
            var loggedInAdmin = new Staff("Cid", "Admin", "cid@skicenter.se", "HashedPassword123", staffRole.SystemAdmin) { StaffID = 99 };
            LogInAs(loggedInAdmin);

            // Act
            var (success, message) = _controller.SetStaffActive(lastAdmin.StaffID, false);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("minst en aktiv systemadministratör"));
            Assert.That(lastAdmin.IsActive, Is.True);
        }

        /// <summary>
        /// Verifies that only system admins can deactivate accounts.
        /// </summary>
        [Test]
        [TestCase(staffRole.SkiShop)]
        [TestCase(staffRole.BookingAdmin)]
        [TestCase(staffRole.MarketingManager)]
        public void SetStaffActive_NotSystemAdmin_ReturnsFalse(staffRole role)
        {
            // Arrange
            var user = new Staff("Test", "Person", "test@skicenter.se", "HashedPassword123", role) { StaffID = 50 };
            LogInAs(user);

            // Act
            var (success, _) = _controller.SetStaffActive(_skiShopEmployee.StaffID, false);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(_skiShopEmployee.IsActive, Is.True);

            // Verify
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Verifies that a deactivated account cannot log in.
        /// </summary>
        [Test]
        public void Login_DeactivatedAccount_IsRefused()
        {
            // Arrange
            _skiShopEmployee.IsActive = false;

            // Act
            _mockStaffRepo
                .Setup(r => r.GetStaffByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_skiShopEmployee);
            var (staff, message) = _loginController.Login(_skiShopEmployee.Email, "AnyPassword123");

            // Assert
            Assert.That(staff, Is.Null);
            Assert.That(message, Does.Contain("inaktiverat"));
            Assert.That(LoginController.CurrentLoggedInUser, Is.Not.SameAs(_skiShopEmployee));
        }
    }
}
