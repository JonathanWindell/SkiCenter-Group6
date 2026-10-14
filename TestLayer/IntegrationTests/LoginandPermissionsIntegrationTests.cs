using BusinessLayer;
using BusinessLayer.Controllers;
using DataLayer;
using DataLayer.Interfaces;
using DataLayer.Repositories;
using EntityLayer;
using Microsoft.EntityFrameworkCore.Storage;
using NUnit.Framework;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class LoginAndPermissionTests
    {
        private SkiCenterDbContext _context;
        private IDbContextTransaction _transaction;
        private LoginController _loginController;

        [SetUp]
        public void Setup()
        {
            // Initialize database context
            _context = new SkiCenterDbContext();

            // Begin a transaction to isolate test mutations from the actual database
            _transaction = _context.Database.BeginTransaction();

            // Wire up dependencies across architectural layers
            var _unitOfWork = new UnitOfWork(_context); // Can eb local due to _unitOfWork not being directly in tests
            _loginController = new LoginController(_unitOfWork);

            // Ensure any existing session state is cleared
            _loginController.Logout();
        }

        [TearDown]
        public void TearDown()
        {
            // Roll back all changes made during the test to avoid foreign key conflicts
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }

            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        /// Verifies that an existing BookingAdmin in the database can log in with valid credentials,
        /// returns the user entity, and sets the active session.
        /// </summary>
        [Test]
        public void BookingAdminExists_ValidData_ReturnsTrue()
        {
            // Arrange: Seed an existing user with a hashed password in the database
            string rawPassword = "TestBookingAdminPassword";
            string dbHashedPassword = HashingService.HashPassword(rawPassword);
            string email = "testadmin_unique@test.se";

            var existingStaff = new Staff("Johan", "Windell", email, dbHashedPassword, staffRole.BookingAdmin);
            _context.StaffMembers.Add(existingStaff);
            _context.SaveChanges();

            // Act: Attempt login via LoginController using the raw password
            var (loggedInStaff, statusMsg) = _loginController.Login(email, rawPassword);

            // Assert: Verify database match, returned entity, and active session
            Assert.That(loggedInStaff, Is.Not.Null, "Expected valid Staff entity on successful authentication.");
            Assert.That(loggedInStaff.Email, Is.EqualTo(email));
            Assert.That(loggedInStaff.Role, Is.EqualTo(staffRole.BookingAdmin));
            Assert.That(LoginController.CurrentLoggedInUser, Is.Not.Null, "CurrentLoggedInUser must be populated upon successful login.");
            Assert.That(statusMsg, Is.EqualTo("Login successful."));
        }

        /// <summary>
        /// Verifies that invalid credentials fail database authentication,
        /// return null, and do not create an active session.
        /// </summary>
        [Test]
        public void BookingAdminExists_NonValidData_ReturnsFalse()
        {
            // Arrange: Seed an existing user in the database
            string correctPassword = "TestBookingAdminPassword";
            string wrongPassword = "WrongBookingAdminPassword";
            string email = "testadmin_unique@test.se";

            var existingStaff = new Staff("Johan", "Windell", email, HashingService.HashPassword(correctPassword), staffRole.BookingAdmin);
            _context.StaffMembers.Add(existingStaff);
            _context.SaveChanges();

            // Act: Attempt login with incorrect password
            var (loggedInStaff, statusMsg) = _loginController.Login(email, wrongPassword);

            // Assert: Verify rejection and absence of session
            Assert.That(loggedInStaff, Is.Null, "Expected null Staff entity when invalid credentials are provided.");
            Assert.That(LoginController.CurrentLoggedInUser, Is.Null, "CurrentLoggedInUser must remain null when authentication fails.");
            Assert.That(statusMsg, Is.EqualTo("Invalid email or password."));
        }

        /// <summary>
        /// Verifies that an authenticated SkiShop staff member is denied authorization
        /// to change season prices according to role-based access control (RBAC).
        /// </summary>
        [Test]
        public void SkiShopUser_AccessSeasonPrices_ReturnsFalse()
        {
            // Arrange: Seed and authenticate a SkiShop user through the database and controller
            string password = "TestSkiShopPassword";
            string email = "testskishop_unique@test.se";

            var shopStaff = new Staff("Vincent", "Lind", email, HashingService.HashPassword(password), staffRole.SkiShop);
            _context.StaffMembers.Add(shopStaff);
            _context.SaveChanges();

            // Authenticate user to establish an active session
            _loginController.Login(email, password);
            Assert.That(LoginController.CurrentLoggedInUser, Is.Not.Null, "Precondition failed: SkiShop user must be logged in.");

            // Act: Evaluate permission for the active session user's role
            bool hasAccessToSeasonPrices = StaffController.CanChangeSeasonPrices(LoginController.CurrentLoggedInUser.Role);

            // Assert: Verify permission rejection for SkiShop role
            Assert.That(hasAccessToSeasonPrices, Is.False, "Ski-shop staff must be denied access to season prices.");
        }
    }
}