using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class AccommodationRepository : Repository<Accommodation>, IAccommodationRepository
    {
        #region Constructor & Fields

        private readonly SkiCenterDbContext _context;

        public AccommodationRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        #endregion

        #region Availability & Overlap Queries

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
                             ba.Booking.BookingStatus != BookingStatus.Cancelled &&
                             ba.StartDate < endDate &&
                             ba.EndDate > startDate)
                .Select(ba => ba.AccommodationID)
                .Distinct();

            return _context.Set<Accommodation>()
                .Include(a => a.AccommodationType)
                .Where(a => !bookedAccommodationIds.Contains(a.AccommodationID) &&
                    a.AccommodationType.NumberOfBeds >= numberOfBeds &&
                    a.Status == AccommodationStatus.Available)
                 .AsNoTracking()
                 .ToList();
        }

        /// <summary>
        /// Gets available accommodations when editing an existing booking.
        /// Excludes the current booking from the overlap check.
        /// </summary>
        public IEnumerable<Accommodation> GetAvailableAccommodationsForEdit(
            DateTime startDate,
            DateTime endDate,
            int numberOfBeds,
            int bookingId)
        {
            if (startDate.Date <= DateTime.Today ||
                startDate >= endDate ||
                numberOfBeds <= 0 ||
                bookingId <= 0)
            {
                return Enumerable.Empty<Accommodation>();
            }

            // Find accommodations booked by OTHER bookings
            // during the selected period.
            var bookedAccommodationIds = _context.Set<BookingAccommodation>()
                .Where(ba =>
                    ba.BookingID != bookingId &&
                    ba.Booking != null &&
                    ba.Booking.BookingStatus != BookingStatus.Cancelled &&
                    ba.StartDate < endDate &&
                    ba.EndDate > startDate)
                .Select(ba => ba.AccommodationID)
                .Distinct();

            // Return available accommodations with enough beds.
            return _context.Set<Accommodation>()
                .Include(a => a.AccommodationType)
                .Where(a =>
                    !bookedAccommodationIds.Contains(a.AccommodationID) &&
                    a.AccommodationType.NumberOfBeds >= numberOfBeds &&
                    a.Status == AccommodationStatus.Available)
                .AsNoTracking()
                .ToList();
        }

        #endregion

        #region Read All & Category Queries

        /// <summary>
        /// Gets all accommodations that are registered in the system.
        /// </summary>
        public IEnumerable<Accommodation> GetAllAccommodations()
        {
            return _context.Set<Accommodation>()
                .Include(a => a.AccommodationType)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all accommodations in a specific category.
        /// </summary>
        /// <param name="category"></param>
        public IEnumerable<AccommodationType> GetAccommodationsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<AccommodationType>();
            }

            return _context.Set<AccommodationType>()
                .Where(a => a.CategoryCode == category.Trim())
                .AsNoTracking()
                .ToList();
        }

        #endregion
    }
}