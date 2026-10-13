using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface ISeasonPriceRepository : IRepository<SeasonPrice>
    {
        SeasonPrice GetSeasonPrice(int accommodationTypeId, string priceType, int week);

        void UpdatePrice(SeasonPrice seasonPrice);

        IEnumerable<SeasonPrice> GetPricesByWeek(int week);

        IEnumerable<SeasonPrice> GetPricesByType(int accommodationTypeId);

        IEnumerable<SeasonPrice> GetCurrentPrices(int? accommodationTypeId = null, string? priceType = null, int? week = null);

        IEnumerable<SeasonPrice> GetPriceHistory(int accommodationTypeId, string priceType, int week);
    }
}
