using DataLayer;
using BusinessLayer;
using DataLayer.Interfaces;
using EntityLayer;
using System;

namespace BusinessLayer.Controllers
{

    /// <summary>
    /// Handles user authentication, input validation, and login operations.
    /// Orchestrates the communication between the UI and the Data Access Layer for security purposes.
    /// </summary>
    public class LoginController
    {
        private readonly IUnitOfWork _unitOfWork;

        // Keeps track of current logged in staff. 
        public static Staff CurrentLoggedInUser { get; private set; }

        public LoginController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Verifies the user's credentials against the specific repository based on the user type.
        /// </summary>
        /// <param name="email">The email to search for.</param>
        /// <param name="plainTextPassword">The plain-text password to hash and compare.</param>
        /// <returns>The found <see cref="CurrentLoggedInUser"/> object if credentials match; otherwise, null.</returns>
        public (Staff? staff, string statusMsg) Login(string email, string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(plainTextPassword))
            {
                return (false, "Email and password are required.");
            }

            string hashedPassword = HashingPassword.HashPassword(plainTextPassword);

            Staff foundStaff = _unitOfWork.Staff.GetStaffByEmailAndPassword(email, hashedPassword);

            if (foundStaff != null)
            {
                CurrentLoggedInUser = foundStaff;
                return true;
            }

            return false;

        }

        // Sets current user to null to remove session
        public void Logout()
        {
            CurrentLoggedInUser = null;
        }
    }
}
