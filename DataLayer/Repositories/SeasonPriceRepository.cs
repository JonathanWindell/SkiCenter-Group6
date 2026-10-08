using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    /// <summary>
    /// Season prices are stored as history. Every price change is a new row, and the row with
    /// the latest ValidFrom for an accommodation type, price type and week is the current price.
    /// </summary>
    public class SeasonPriceRepository : Repository<SeasonPrice>, ISeasonPriceRepository
    {
        private readonly SkiCenterDbContext _context;

        public SeasonPriceRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the current season price for a specific accommodation type, price type and week.
        /// </summary>
        /// <param name="accommodationTypeId"></param>
        /// <param name="priceType"></param>
        /// <param name="week"></param>
        public SeasonPrice GetSeasonPrice(int accommodationTypeId, string priceType, int week)
        {
            if (accommodationTypeId <= 0 || string.IsNullOrWhiteSpace(priceType) || week <= 0)
            {
                return null;
            }

            string type = priceType.Trim();

            return _context.Set<SeasonPrice>()
                .Include(sp => sp.AccommodationType)
                .Include(sp => sp.ChangedBy)
                .Where(sp => sp.AccommodationTypeID == accommodationTypeId && sp.PriceType == type && sp.WeekNumber == week)
                .OrderByDescending(sp => sp.ValidFrom)
                .ThenByDescending(sp => sp.SeasonPriceID)
                .AsNoTracking()
                .FirstOrDefault();
        }

        /// <summary>
        /// Updates a specific season price.
        /// Note: overwrites the price history. Use Add with a new SeasonPrice to register a price change.
        /// </summary>
        /// <param name="seasonPrice"></param>
        public void UpdatePrice(SeasonPrice seasonPrice)
        {
            _context.Set<SeasonPrice>().Update(seasonPrice);
        }

        /// <summary>
        /// Gets all current prices for a specfic week.
        /// </summary>
        /// <param name="week"></param>
        public IEnumerable<SeasonPrice> GetPricesByWeek(int week)
        {
            if (week <= 0)
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return GetCurrentPrices(null, null, week);
        }

        /// <summary>
        /// Gets all current season prices for a specific type of accommodation.
        /// </summary>
        /// <param name="accommodationTypeId"></param>
        public IEnumerable<SeasonPrice> GetPricesByType(int accommodationTypeId)
        {
            if (accommodationTypeId <= 0)
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return GetCurrentPrices(accommodationTypeId, null, null);
        }

        /// <summary>
        /// Gets the current price for every accommodation type, price type and week, optionally filtered.
        /// A price is current when no newer price exists for the same accommodation type, price type and week.
        /// </summary>
        /// <param name="accommodationTypeId">Optional filter on accommodation type.</param>
        /// <param name="priceType">Optional filter on price type, e.g. "vecka".</param>
        /// <param name="week">Optional filter on week number.</param>
        public IEnumerable<SeasonPrice> GetCurrentPrices(int? accommodationTypeId = null, string? priceType = null, int? week = null)
        {
            IQueryable<SeasonPrice> prices = _context.Set<SeasonPrice>();

            if (accommodationTypeId.HasValue)
            {
                prices = prices.Where(sp => sp.AccommodationTypeID == accommodationTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(priceType))
            {
                string type = priceType.Trim();
                prices = prices.Where(sp => sp.PriceType == type);
            }

            if (week.HasValue)
            {
                prices = prices.Where(sp => sp.WeekNumber == week.Value);
            }

            return prices
                .Where(sp => !_context.Set<SeasonPrice>().Any(newer =>
                    newer.AccommodationTypeID == sp.AccommodationTypeID &&
                    newer.PriceType == sp.PriceType &&
                    newer.WeekNumber == sp.WeekNumber &&
                    (newer.ValidFrom > sp.ValidFrom ||
                     (newer.ValidFrom == sp.ValidFrom && newer.SeasonPriceID > sp.SeasonPriceID))))
                .Include(sp => sp.AccommodationType)
                .Include(sp => sp.ChangedBy)
                .OrderBy(sp => sp.WeekNumber)
                .ThenBy(sp => sp.AccommodationTypeID)
                .ThenBy(sp => sp.PriceType)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets every registered price for an accommodation type, price type and week, newest first.
        /// </summary>
        /// <param name="accommodationTypeId"></param>
        /// <param name="priceType"></param>
        /// <param name="week"></param>
        public IEnumerable<SeasonPrice> GetPriceHistory(int accommodationTypeId, string priceType, int week)
        {
            if (accommodationTypeId <= 0 || string.IsNullOrWhiteSpace(priceType) || week <= 0)
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            string type = priceType.Trim();

            return _context.Set<SeasonPrice>()
                .Include(sp => sp.ChangedBy)
                .Where(sp => sp.AccommodationTypeID == accommodationTypeId && sp.PriceType == type && sp.WeekNumber == week)
                .OrderByDescending(sp => sp.ValidFrom)
                .ThenByDescending(sp => sp.SeasonPriceID)
                .AsNoTracking()
                .ToList();
        }
    }
}
