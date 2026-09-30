using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets a specific booking with all its associated data.
        /// </summary>
        /// <param name="bookingID"></param>
        public Booking GetSpecificBooking(int bookingID)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.BookingAccommodations)
                    .ThenInclude(ba => ba.Accommodation)
                .Include(b => b.BookingMeetingRooms)
                    .ThenInclude(bm => bm.MeetingRoom)
                .Include(b => b.SkiLessonBookings)
                    .ThenInclude(sl => sl.SkiLessonSession)
                .Include(b => b.Rentals)
                    .ThenInclude(r => r.RentalItems)
                        .ThenInclude(ri => ri.EquipmentItem)
                .Include(b => b.Invoices)
                .AsSplitQuery()
                .FirstOrDefault(b => b.BookingID == bookingID);
        }

        /// <summary>
        /// Gets bookings where accommodation or activitydate overlaps the search criteria. 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        public IEnumerable<Booking> GetBookingsInRange(DateOnly startDate, DateOnly endDate)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Include(b => b.BookingAccommodations)
                    .ThenInclude(ba => ba.Accommodation)
                .Where(b => b.BookingAccommodations.Any(ba =>
                    ba.StartDate < endDate && ba.EndDate > startDate))
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        public IEnumerable<Booking> GetBookingsByStatus(string status)
        {
            return _context.Set<Booking>()
                .Include(b => b.Customer)
                .Where(b => b.Status == status)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Booking> GetBookingsByCustomer(int customerID)
        {
            return _context.Set<Booking>()
                .Include(b => b.BookingAccommodations)
                .Where(b => b.CustomerID == customerID)
                .AsNoTracking()
                .ToList();
        }

        public void UpdateBookingStatus(int bookingID, string status)
        {
            var booking = _context.Set<Booking>().Find(bookingID);
            if (booking != null)
            {
                booking.Status = status;
            }
        }
    }
}
