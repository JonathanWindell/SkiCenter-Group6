using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface ISeasonPriceRepository : IRepository<SeasonPrice>
    {
        SeasonPrice GetSeasonPrice(string accommodationType, int week);

        void UpdatePrice(SeasonPrice seasonPrice);

        IEnumerable<SeasonPrice> GetPricesByWeek(int week);

        IEnumerable<SeasonPrice> GetPricesByType(string accommodationType);
    }
}
