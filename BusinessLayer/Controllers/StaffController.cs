using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class StaffController
    {
        #region Fields and role names

        private readonly IUnitOfWork _unitOfWork;

        // Swedish display names for the staff roles, used everywhere a role is shown to the user
        private static readonly Dictionary<staffRole, string> RoleDisplayNames = new()
        {
            [staffRole.BookingAdmin] = "Bokningsadministratör",
            [staffRole.SkiShop] = "Skidbutik",
            [staffRole.MarketingManager] = "Marknadschef",
            [staffRole.SystemAdmin] = "Systemadministratör"
        };

        /// <summary>
        /// Returns the role's Swedish name, e.g. "Bokningsadministratör" for BookingAdmin.
        /// </summary>
        public static string GetRoleDisplayName(staffRole role)
        {
            return RoleDisplayNames.TryGetValue(role, out string? name) ? name : role.ToString();
        }

        #endregion

        #region Constructor

        public StaffController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Access checks

        /// <summary>
        /// Verifies that current user has SystemAdmin role. 
        /// This function is used to validate if user can access view and change season prices. 
        /// </summary>
        public static bool CanChangeSeasonPrices(staffRole currentRole)
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.SystemAdmin)
            {
                return true; // Allow Access
            }

            return false; //Deny Access;
        }

        /// <summary>
        /// Verifies that current user has BookingAdmin role. 
        /// This function is used to validate if user can access view, change and create booking for customer. 
        /// </summary>
        public static bool CanCreateBooking(staffRole currentRole)
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.BookingAdmin)
            {
                return true; // Allow Access
            }

            return false; //Deny Access;
        }

        /// <summary>
        /// Verifies that current user has SkiShop role. 
        /// This function is used to validate if user can access view and handle equipment and/ or remove equipment on customers booking. . 
        /// </summary>
        public static bool CanCreateEquipment(staffRole currentRole)
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.SkiShop)
            {
                return true; // Allow Access
            }

            return false; //Deny Access;
        }

        /// <summary>
        /// Verifies that current user has MarketinManager role. 
        /// This function is used to validate if user can access view and set credit and discount limits for corporate customers. 
        /// </summary>
        public static bool CanSetCreditAndDiscountLimits(staffRole currentRole)
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.MarketingManager)
            {
                return true; // Allow Access
            }

            return false; //Deny Access;
        }

        /// <summary>
        /// Verifies that the role may generate invoicing exports.
        /// Allowed for MarketingManager, and for SystemAdmin who can access every view.
        /// </summary>
        public static bool CanExportInvoices(staffRole currentRole)
        {
            return currentRole == staffRole.MarketingManager || currentRole == staffRole.SystemAdmin;
        }

        /// <summary>
        /// Verifies that the role may view the statistics dashboard.
        /// Allowed for MarketingManager, and for SystemAdmin who can access every view.
        /// </summary>
        public static bool CanViewStatistics(staffRole currentRole)
        {
            return currentRole == staffRole.MarketingManager || currentRole == staffRole.SystemAdmin;
        }

        /// <summary>
        /// Verifies that user is SystemAdmin and can thererfore manage personel.
        /// </summary>
        public static bool CanManageStaff(staffRole currentRole)
        {
            return currentRole == staffRole.SystemAdmin;
        }

        #endregion

        #region Create staff

        /// <summary>
        /// Creates a new user and hashes password.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="password"></param>
        /// <param name="role"></param>
        public (bool success, string message) CreateStaff(string firstName, string lastName, string email, string password, staffRole role)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Alla fält måste fyllas i.");
            }

            if (password.Length < 10)
            {
                return (false, "Lösenordet måste vara minst 10 tecken långt.");
            }

            if (_unitOfWork.Staff.EmailExists(email.Trim()))
            {
                return (false, "En användare med denna e-postadress finns redan.");
            }

            // Hash password before object is built. 
            string hashedPassword = HashingService.HashPassword(password);

            var newStaff = new Staff(
                firstName.Trim(),
                lastName.Trim(),
                email.Trim().ToLower(),
                hashedPassword,
                role
            );

            _unitOfWork.Staff.Add(newStaff);

            if (_unitOfWork.Complete() > 0)
            {
                return (true, $"{firstName} {lastName} har registrerats som {GetRoleDisplayName(role).ToLower()}.");
            }

            return (false, "Ett fel uppstod när användaren skulle sparas i databasen.");
        }

        #endregion
    }
}
