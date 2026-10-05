using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class StaffController
    {
        public static bool CheckAccessToSeasonPrices(staffRole currentRole)
        {
            return DashboardController.HasAccess(currentRole, SystemModule.SeasonPrices);
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
