using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IBookingAccommodationRepository : IRepository<BookingAccommodation>
    {
        IEnumerable<BookingAccommodation> GetBookedAccommodations(string status);

        bool HasOverlappedBooking(DateTime startDate, DateTime endDate, int accommodationID);

        void UpdateBookingAccommodation(BookingAccommodation bookingAccommodation);
    }
}
