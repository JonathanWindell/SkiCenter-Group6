using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer
{
    public class BookingController // WIP
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves all bookings with their navigation properties. 
        /// (Customer, Accommodations, MeetingRooms, SkiLessons, Rentals and Invoice). 
        /// </summary>
        /// <returns>A collection of all bookings.</returns>
        public IEnumerable<Booking> GetAllBookings()
        {
            return _unitOfWork.Booking.GetAllBookings();
        }

        /// <summary>
        /// Retrieves a specific booking with all  details by its unique BookingID.
        /// </summary>
        /// <param name="bookingId"/param>
        /// <returns>The complete booking entity, or null if not found.</returns>
        public Booking GetSpecificBooking(int bookingID)
        {
            if (bookingID <= 0) return null;
            return _unitOfWork.Booking.GetSpecificBooking(bookingID);
        }

        /// <summary>
        /// Retrieves all bookings that fall within a specified date interval.
        /// </summary>
        /// <param name="startDate"/param>
        /// <param name="endDate"/param>
        /// <returns>A collection of matching bookings.</returns>
        public IEnumerable<Booking> GetBookingsInRange(DateTime startDate, DateTime endDate)
        {
            if (startDate == DateTime.MinValue || endDate == DateTime.MinValue || startDate >= endDate)
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsInRange(startDate, endDate);
        }

        /// <summary>
        /// Retrieves all bookings filtered by status.
        /// (e.g., "Preliminary", "Confirmed", "Cancelled").
        /// </summary>
        /// <param name="status"/param>
        /// <returns>A collection of bookings with the specified status.</returns>
        public IEnumerable<Booking> GetBookingsByStatus(BookingStatus status)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), status))
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsByStatus(status);
        }

        /// <summary>
        /// Retrieves booking history for a specific customer.
        /// </summary>
        /// <param name="customerId"/param>
        /// <returns>A collection of bookings belonging to the customer.</returns>
        public IEnumerable<Booking> GetBookingsByCustomer(int customerId)
        {
            if (customerId <= 0) return Enumerable.Empty<Booking>();
            return _unitOfWork.Booking.GetBookingsByCustomer(customerId);
        }

        //
        // WIP, MISSING FUNCTIONS

    }
}
