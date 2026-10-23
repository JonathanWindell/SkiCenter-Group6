
using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IAccommodationRepository : IRepository<Accommodation>
    {
        // Get available accommodations for a new booking.
        IEnumerable<Accommodation> GetAvailableAccommodations(
            DateTime startDate,
            DateTime endDate,
            int numberOfBeds);

        // Get available accommodations when editing an existing booking.
        // Excludes the current booking from the availability check.
        IEnumerable<Accommodation> GetAvailableAccommodationsForEdit(
            DateTime startDate,
            DateTime endDate,
            int numberOfBeds,
            int bookingId);

        // Get all accommodations.
        IEnumerable<Accommodation> GetAllAccommodations();

        // Get accommodations by category.
        IEnumerable<AccommodationType> GetAccommodationsByCategory(
            string category);
    }
}
