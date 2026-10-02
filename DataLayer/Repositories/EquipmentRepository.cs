using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        private readonly SkiCenterDbContext _context;

        public EquipmentRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
