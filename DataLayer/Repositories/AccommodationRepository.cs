using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="numberOfBeds"></param>
        public IEnumerable<Accommodation> GetAvailableAccommodations(DateTime startDate, DateTime endDate, int numberOfBeds)
        {
            if (startDate >= endDate || numberOfBeds <= 0)
            {
                return Enumerable.Empty<Accommodation>();
            }

            var bookedAccommodationIds = _context.Set<BookingAccommodation>()
                .Where(ba => ba.Booking != null &&
                             ba.Booking.Status != "Cancelled" &&
                             ba.StartDate < endDate &&
                             ba.EndDate > startDate)
                .Select(ba => ba.AccommodationID)
                .Distinct();

            return _context.Set<Accommodation>()
                .Where(a => a.NumberOfBeds >= numberOfBeds &&
                            a.Status == "Available" &&
                            !bookedAccommodationIds.Contains(a.AccommodationID))
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all accommodations that are registered in the system.
        /// </summary>
        public IEnumerable<Accommodation> GetAllAccommodations()
        {
            return _context.Set<Accommodation>()
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all accommodations in a specific category.
        /// </summary>
        /// <param name="category"></param>
        public IEnumerable<Accommodation> GetAccommodationsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<Accommodation>();
            }

            return _context.Set<Accommodation>()
                .Where(a => a.Category == category.Trim())
                .AsNoTracking()
                .ToList();
        }
    }
}
