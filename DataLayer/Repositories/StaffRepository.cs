using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        private readonly SkiCenterDbContext _context;

        public StaffRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Fetches staff by email and password to validate login information. 
        /// </summary>
        public Staff GetStaffByEmailAndPassword(string email, string password)
        {
            return _context.StaffMembers.FirstOrDefault(s => s.Email == email && s.Password == password);
        }
    }
}
