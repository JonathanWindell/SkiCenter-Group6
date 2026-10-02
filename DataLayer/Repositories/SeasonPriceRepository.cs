using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class SeasonPriceRepository : Repository<SeasonPrice>, ISeasonPriceRepository
    {
        private readonly SkiCenterDbContext _context;

        public SeasonPriceRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a specific season price based on type and week.
        /// </summary>
        /// <param name="accommodationType"></param>
        /// <param name="week"></param>
        public SeasonPrice GetSeasonPrice(string accommodationType, int week)
        {
            if (string.IsNullOrWhiteSpace(accommodationType) || week <= 0)
            {
                return null;
            }

            return _context.Set<SeasonPrice>()
                .FirstOrDefault(sp => sp.AccommodationType == accommodationType.Trim() && sp.WeekNumber == week);
        }

        /// <summary>
        /// Updates a specific season price.
        /// </summary>
        /// <param name="seasonPrice"></param>
        public void UpdatePrice(SeasonPrice seasonPrice)
        {
            _context.Set<SeasonPrice>().Update(seasonPrice);
        }

        /// <summary>
        /// Gets all prices for a specfic week.
        /// </summary>
        /// <param name="week"></param>
        public IEnumerable<SeasonPrice> GetPricesByWeek(int week)
        {
            if (week <= 0)
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return _context.Set<SeasonPrice>()
                .Where(sp => sp.WeekNumber == week)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all season prices for a specific type of accommodation.
        /// </summary>
        /// <param name="accommodationType"></param>
        public IEnumerable<SeasonPrice> GetPricesByType(string accommodationType)
        {
            if (string.IsNullOrWhiteSpace(accommodationType))
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return _context.Set<SeasonPrice>()
                .Where(sp => sp.AccommodationType == accommodationType.Trim())
                .OrderBy(sp => sp.WeekNumber)
                .AsNoTracking()
                .ToList();
        }
    }
}
