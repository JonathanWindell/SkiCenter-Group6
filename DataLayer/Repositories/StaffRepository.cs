using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        #region Constructor & Fields
        private readonly SkiCenterDbContext _context;

        public StaffRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        #endregion

        #region Validation Methods & Login
        /// <summary>
        /// Gets staff by Email and Password
        /// <summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public Staff? GetStaffByEmailAndPassword(string email, string password)
        {
            return _context.StaffMembers.FirstOrDefault(s => s.Email == email && s.Password == password);
        }

        /// <summary>
        /// Checks if Email is already in use. 
        /// <summary>
        /// <param name="email"></param>
        public bool EmailExists(string email)
        {
            return _context.StaffMembers.Any(s => s.Email.ToLower() == email.ToLower());
        }

        #endregion
    }
}