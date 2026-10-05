using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class RentalRepository : Repository<Rental>, IRentalRepository
    {
        private readonly SkiCenterDbContext _context;

        public RentalRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the specific rental with its equipmentItems connected to it.
        /// </summary>
        /// <param name="rentalID"></param>
        public Rental GetRentalWithItems(int rentalID)
        {
            if (rentalID <= 0) return null;

            return _context.Set<Rental>()
                .Include(r => r.EquipmentItems)
                    .ThenInclude(ei => ei.Equipment)
                .FirstOrDefault(r => r.RentalID == rentalID);
        }

        /// <summary>
        /// Gets all rentals that have the status "Active"
        /// </summary>
        public IEnumerable<Rental> GetActiveRentals()
        {
            return _context.Set<Rental>()
                .Include(r => r.EquipmentItems)
                    .ThenInclude(ei => ei.Equipment)
                .Where(r => r.Status == "Active")
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all rentals for a specific customer, by their ID.
        /// </summary>
        /// <param name="customerID"></param>
        public IEnumerable<Rental> GetRentalsByCustomer(int customerID)
        {
            if (customerID <= 0)
            {
                return Enumerable.Empty<Rental>();
            }

            return _context.Set<Rental>()
                .Include(r => r.EquipmentItems)
                    .ThenInclude(ei => ei.Equipment)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all rentals for a specific booking, by its ID.
        /// </summary>
        /// <param name="bookingID"></param>
        /// <returns></returns>
        public IEnumerable<Rental> GetRentalsByBooking(int bookingID)
        {
            if (bookingID <= 0)
            {
                return Enumerable.Empty<Rental>();
            }

            return _context.Set<Rental>()
                .Include(r => r.EquipmentItems)
                    .ThenInclude(ei => ei.Equipment)
                .AsNoTracking()
                .ToList();
        }
    }
}
