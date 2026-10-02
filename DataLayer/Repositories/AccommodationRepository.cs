using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    /// <summary>
    /// Repository responsible for handling database operations related to Bookings.
    /// Inherits basic CRUD operations from the generic Repository.
    /// </summary>
    public class AccommodationRepository : Repository<Accommodation>, IAccommodationRepository
    {
        private readonly SkiCenterDbContext _context;

        public AccommodationRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
