using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        private readonly SkiCenterDbContext _context;

        public BookingRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all bookings with all its associated data.
        /// </summary>
        public IEnumerable<Booking> GetAllBookings()
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.Accommodations)
                    .ThenInclude(ba => ba.Accommodation)
                .Include(b => b.MeetingRooms)
                .Include(b => b.SkiLessons)
                    .ThenInclude(sl => sl.SkiLessonSession)
                .Include(b => b.Rentals)
                    .ThenInclude(r => r.EquipmentItems)
                        .ThenInclude(ei => ei.Equipment)
                .Include(b => b.Invoices)
                .AsSplitQuery()
                .ToList();
        }

        /// <summary>
        /// Gets a specific booking with all its associated data.
        /// </summary>
        /// <param name="bookingID"></param>
        public Booking? GetSpecificBooking(int bookingID)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.Accommodations)
                    .ThenInclude(ba => ba.Accommodation)
                    .ThenInclude(a => a.AccommodationType)
                .Include(b => b.MeetingRooms)
                .Include(b => b.SkiLessons)
                    .ThenInclude(sl => sl.SkiLessonSession)
                    .ThenInclude(sls => sls.SkiLesson)
                .Include(b => b.Rentals)
                    .ThenInclude(r => r.EquipmentItems)
                        .ThenInclude(ri => ri.Equipment)
                            .ThenInclude(e => e.Prices)
                .Include(b => b.Invoices)
                .AsSplitQuery()
                .FirstOrDefault(b => b.BookingID == bookingID);
        }

        /// <summary>
        /// Gets bookings where accommodation or activitydate overlaps the search criteria. 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        public IEnumerable<Booking> GetBookingsInRange(DateTime startDate, DateTime endDate)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.Accommodations)
                    .ThenInclude(ba => ba.Accommodation)
                .Where(b => b.Accommodations.Any(ba =>
                    ba.StartDate < endDate && ba.EndDate > startDate))
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets booking by a specific status.
        /// </summary>
        /// <param name="status"></param>
        public IEnumerable<Booking> GetBookingsByStatus(BookingStatus status)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Where(b => b.BookingStatus == status)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets bookings by a specific customerID.
        /// </summary>
        /// <param name="customerID"></param>
        public IEnumerable<Booking> GetBookingsByCustomer(int customerID)
        {
            return _context.Set<Booking>()
                .Include(b => b.Accommodations)
                .Where(b => b.CustomerID == customerID)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Finds active room bookings by booking ID or accommodation (room) number.
        /// A booking is active when it is preliminary or confirmed, the guest has not checked out,
        /// and it has an accommodation stay that has not ended yet.
        /// </summary>
        /// <param name="bookingID">Optional booking ID to search for.</param>
        /// <param name="accommodationNumber">Optional room number to search for, e.g. "LI1".</param>
        public IEnumerable<Booking> FindActiveBookings(int? bookingID, string? accommodationNumber)
        {
            string? roomNumber = string.IsNullOrWhiteSpace(accommodationNumber) ? null : accommodationNumber.Trim();

            if (!bookingID.HasValue && roomNumber == null)
            {
                return Enumerable.Empty<Booking>();
            }

            DateTime today = DateTime.Today;

            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.Accommodations)
                    .ThenInclude(ba => ba.Accommodation)
                        .ThenInclude(a => a.AccommodationType)
                .Include(b => b.Rentals)
                    .ThenInclude(r => r.EquipmentItems)
                        .ThenInclude(ei => ei.Equipment)
                .Where(b => (b.BookingStatus == BookingStatus.Preliminary || b.BookingStatus == BookingStatus.Confirmed)
                    && b.CheckInStatus != CheckInStatus.CheckedOut
                    && b.Accommodations.Any(ba => ba.EndDate >= today))
                .Where(b => (bookingID.HasValue && b.BookingID == bookingID.Value)
                    || (roomNumber != null && b.Accommodations.Any(ba => ba.Accommodation.AccommodationNumber == roomNumber)))
                .AsSplitQuery()
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Updates a specific booking with a new status.
        /// </summary>
        /// <param name="bookingID"></param>
        /// <param name="status"></param>
        public void UpdateBookingStatus(int bookingID, BookingStatus status)
        {
            var booking = _context.Set<Booking>().Find(bookingID);

            if (booking != null)
            {
                booking.BookingStatus = status;
                _context.SaveChanges();
            }
        }
    }
}
