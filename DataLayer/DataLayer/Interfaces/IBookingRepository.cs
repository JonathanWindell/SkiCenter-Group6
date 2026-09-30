using System;
using System.Collections.Generic;
using System.Text;
using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IBookingRepository : IRepository<Booking>
    {
        IEnumerable<Booking> GetAllBookings();

        Booking GetSpecificBooking(int bookingID);

        IEnumerable<Booking> GetBookingsInRange(DateOnly startDate, DateOnly endDate);

        IEnumerable<Booking> GetBookingsByStatus(string status);

        IEnumerable<Booking> GetBookingsByCustomer(int customerID);

        void UpdateBookingStatus(int bookingID, string status);
    }
}
