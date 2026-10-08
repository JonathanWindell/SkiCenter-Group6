using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    /// <summary>
    /// Handles system administrator settings, such as season prices.
    /// Only staff with the SystemAdmin role may make changes.
    /// </summary>
    public class AdminController
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets the current season prices, optionally filtered by accommodation type, price type and/or week.
        /// </summary>
        /// <param name="accommodationTypeId">Optional filter on accommodation type.</param>
        /// <param name="priceType">Optional filter on price type, e.g. "vecka".</param>
        /// <param name="week">Optional filter on week number.</param>
        public IEnumerable<SeasonPrice> GetCurrentPrices(int? accommodationTypeId = null, string? priceType = null, int? week = null)
        {
            if (week.HasValue && !IsValidWeek(week.Value))
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return _unitOfWork.SeasonPrice.GetCurrentPrices(accommodationTypeId, priceType, week);
        }

        /// <summary>
        /// Gets every registered price for an accommodation type, price type and week, newest first.
        /// </summary>
        /// <param name="accommodationTypeId"></param>
        /// <param name="priceType"></param>
        /// <param name="week"></param>
        public IEnumerable<SeasonPrice> GetPriceHistory(int accommodationTypeId, string priceType, int week)
        {
            if (accommodationTypeId <= 0 || string.IsNullOrWhiteSpace(priceType) || !IsValidWeek(week))
            {
                return Enumerable.Empty<SeasonPrice>();
            }

            return _unitOfWork.SeasonPrice.GetPriceHistory(accommodationTypeId, priceType, week);
        }

        /// <summary>
        /// Registers a new season price for an accommodation type, price type and week.
        /// The old price is never overwritten; a new row is added so that price history is preserved.
        /// The change is saved in a single database transaction together with who made it and when.
        /// </summary>
        /// <param name="accommodationTypeId"></param>
        /// <param name="priceType"></param>
        /// <param name="week"></param>
        /// <param name="newPrice"></param>
        /// <returns>True and a message if the price was saved, otherwise false and the reason.</returns>
        public (bool success, string message) ChangeSeasonPrice(int accommodationTypeId, string priceType, int week, decimal newPrice)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanChangeSeasonPrices(user.Role))
            {
                return (false, "Endast systemadministratörer kan ändra säsongspriser.");
            }

            if (accommodationTypeId <= 0)
            {
                return (false, "Boendetyp måste anges.");
            }

            if (string.IsNullOrWhiteSpace(priceType))
            {
                return (false, "Pristyp måste anges.");
            }

            if (!IsValidWeek(week))
            {
                return (false, "Veckonummer måste vara mellan 1 och 53.");
            }

            if (newPrice <= 0)
            {
                return (false, "Priset måste vara större än 0.");
            }

            string type = priceType.Trim();
            SeasonPrice? currentPrice = _unitOfWork.SeasonPrice.GetSeasonPrice(accommodationTypeId, type, week);

            if (currentPrice == null)
            {
                return (false, $"Det finns inget pris ({type}) för vald boendetyp vecka {week}.");
            }

            if (currentPrice.Price == newPrice)
            {
                return (false, "Det nya priset är samma som det nuvarande.");
            }

            var priceChange = new SeasonPrice(accommodationTypeId, type, week, newPrice, DateTime.Now, user.StaffID);
            _unitOfWork.SeasonPrice.Add(priceChange);

            if (_unitOfWork.Complete() <= 0)
            {
                return (false, "Priset kunde inte sparas.");
            }

            string typeName = currentPrice.AccommodationType?.CategoryCode ?? $"Boendetyp {accommodationTypeId}";
            return (true, $"Priset för {typeName} ({type}) vecka {week} ändrades från {currentPrice.Price:N2} till {newPrice:N2} kr.");
        }

        private static bool IsValidWeek(int week)
        {
            return week >= 1 && week <= 53;
        }
    }
}
