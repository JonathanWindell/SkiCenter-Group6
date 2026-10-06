using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        private readonly SkiCenterDbContext _context;

        public EquipmentRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all equipment by category. 
        /// </summary>
        /// <param name="category"></param>
        public IEnumerable<Equipment> GetByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<Equipment>();
            }

            return _context.Set<Equipment>()
                .Where(e => e.Category == category.Trim())
                .AsNoTracking()
                .ToList();
        }
    }
}
