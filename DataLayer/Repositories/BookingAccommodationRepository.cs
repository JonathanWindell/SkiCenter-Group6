using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class BookingAccommodationRepository : Repository<BookingAccommodation>, IBookingAccommodationRepository
    {
        private readonly SkiCenterDbContext _context;

        public BookingAccommodationRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all booked accommodation by the determined status.
        /// </summary>
        /// <param name="status"></param>
        public IEnumerable<BookingAccommodation> GetBookedAccommodations(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Enumerable.Empty<BookingAccommodation>();
            }

            return _context.Set<BookingAccommodation>()
                .Include(ba => ba.Accommodation)
                .Include(ba => ba.Booking)
                .Where(ba => ba.Booking != null && ba.Booking.Status == status.Trim())
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Checks if the searched accommodation is already booked in the given dates. 
        /// Excludes cancelled bookings.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="accommodationID"></param>
        public bool HasOverlappedBooking(DateTime startDate, DateTime endDate, int accommodationID)
        {
            if (startDate == DateTime.MinValue || endDate == DateTime.MinValue || startDate >= endDate || accommodationID <= 0)
            {
                return false;
            }

            return _context.Set<BookingAccommodation>()
                .Include(ba => ba.Booking)
                .Any(ba => ba.AccommodationID == accommodationID &&
                           ba.Booking != null &&
                           ba.Booking.Status != "Cancelled" &&
                           ba.StartDate < endDate &&
                           ba.EndDate > startDate);
        }

        /// <summary>
        /// Updates the BookingAccommodation with new data.
        /// </summary>
        /// <param name="bookingAccommodation"></param>
        public void UpdateBookingAccommodation(BookingAccommodation bookingAccommodation)
        {
            _context.Set<BookingAccommodation>().Update(bookingAccommodation);
        }
    }
}
