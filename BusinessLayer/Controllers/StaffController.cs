using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class StaffController
    {
        public static bool CheckAccessToSeasonPrices(staffRole currentRole)
        {
            if (currentRole == staffRole.SystemAdmin)
            {
                return true;
            }
            return false;
        }

        /*
        public bool CanChangeSeasonPrices()
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.SystemAdmin)
            {
                return true; // Allow Access
            }

            return false; //Deny Access;
        }
        */

    }
}
