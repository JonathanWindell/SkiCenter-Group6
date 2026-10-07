using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        private readonly SkiCenterDbContext _context;

        public InvoiceRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a specific booking with all its associated data and calculates total price using AmountExcl and Moms
        /// </summary>
        public Invoice CalculateAmountInclusiveMoms()
        {
            return null;
        }
    }
}
