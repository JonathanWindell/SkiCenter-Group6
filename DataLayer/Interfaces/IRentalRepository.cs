using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IRentalRepository : IRepository<Rental>
    {
        Rental GetRentalWithItems(int rentalID);
        IEnumerable<Rental> GetActiveRentals();
        IEnumerable<Rental> GetRentalsByCustomer(int customerID);
        IEnumerable<Rental> GetRentalsByBooking(int bookingID);
    }
}
