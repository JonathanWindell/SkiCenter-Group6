using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IBookingRepository : IRepository<Booking>
    {
        IEnumerable<Booking> GetAllBookings();

        Booking GetSpecificBooking(int bookingID);

        IEnumerable<Booking> GetBookingsInRange(DateTime startDate, DateTime endDate);

        IEnumerable<Booking> GetBookingsByStatus(BookingStatus status);

        IEnumerable<Booking> GetBookingsByCustomer(int customerID);
    }
}
