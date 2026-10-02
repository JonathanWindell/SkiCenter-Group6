using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IAccommodationRepository : IRepository<Accommodation>
    {
        IEnumerable<Accommodation> GetAvailableAccommodations(DateTime startDate, DateTime endDate, int numberOfBeds);

        IEnumerable<Accommodation> GetAllAccommodations();

        IEnumerable<Accommodation> GetAccommodationsByCategory(string category);

        void UpdateAccommodation(Accommodation accommodation);
    }
}
