using EntityLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.Controllers
{
    public class StaffController
    {


        public bool CanChangeSeasonPrices()
        {
            var user = LoginController.CurrentLoggedInUser;

            // Security Check: Is someone logged in and is it the correct role?
            if (user != null && user.Role == staffRole.SystemAdmin)
            {
                return true; // Tillåt åtkomst
            }

            return false; Deny Access;
        }
    }
}
 