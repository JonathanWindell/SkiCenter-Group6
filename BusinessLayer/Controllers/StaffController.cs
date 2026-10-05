using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class StaffController
    {
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

    }
}
