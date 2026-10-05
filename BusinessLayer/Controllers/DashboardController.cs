using EntityLayer;

namespace BusinessLayer.Controllers
{
    /// <summary>
    /// The parts of the system a staff member can be given access to.
    /// Each module is shown as a button on the dashboard.
    /// </summary>
    public enum SystemModule
    {
        CustomerRegistration,
        EquipmentManagement,
        Rentals,
        Bookings,
        Accommodation,
        SkiSchool,
        SeasonPrices,
        Reports,
        StaffAdmin
    }

    /// <summary>
    /// Decides which system modules each staff role is allowed to access.
    /// The dashboard uses this to build its menu, and other controllers can use it for access checks.
    /// </summary>
    public class DashboardController
    {
        // Single source of truth for role-based access. Add or remove modules for a role here.
        private static readonly Dictionary<staffRole, SystemModule[]> _moduleAccess = new()
        {
            [staffRole.SkiShop] = new[]
            {
                SystemModule.EquipmentManagement,
                SystemModule.Rentals
            },
            [staffRole.BookingAdmin] = new[]
            {
                SystemModule.Bookings,
                SystemModule.CustomerRegistration,
                SystemModule.Accommodation,
                SystemModule.SkiSchool
            },
            [staffRole.MarketingManager] = new[]
            {
                SystemModule.Reports,
                SystemModule.SeasonPrices
            },
            // System admins have access to every module
            [staffRole.SystemAdmin] = Enum.GetValues<SystemModule>()
        };

        /// <summary>
        /// Returns the modules the given role is allowed to access.
        /// </summary>
        public static IReadOnlyList<SystemModule> GetAllowedModules(staffRole role)
        {
            return _moduleAccess.TryGetValue(role, out SystemModule[]? modules)
                ? modules
                : Array.Empty<SystemModule>();
        }

        /// <summary>
        /// Checks whether the given role is allowed to access a specific module.
        /// </summary>
        public static bool HasAccess(staffRole role, SystemModule module)
        {
            return GetAllowedModules(role).Contains(module);
        }

        /// <summary>
        /// Returns the modules the currently logged in staff member is allowed to access.
        /// Returns an empty list if no one is logged in.
        /// </summary>
        public IReadOnlyList<SystemModule> GetModulesForCurrentUser()
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null)
            {
                return Array.Empty<SystemModule>();
            }

            return GetAllowedModules(user.Role);
        }
    }
}
