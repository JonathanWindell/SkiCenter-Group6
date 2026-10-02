using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class BookingAccommodationRepository : Repository<BookingAccommodation>, IBookingAccommodationRepository
    {
        private readonly SkiCenterDbContext _context;

        public BookingAccommodationRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
