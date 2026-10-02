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
    }
}
