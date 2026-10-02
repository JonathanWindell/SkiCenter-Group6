using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class RentalRepository : Repository<Rental>, IRentalRepository
    {
        private readonly SkiCenterDbContext _context;

        public RentalRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
