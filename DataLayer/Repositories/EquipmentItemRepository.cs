using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class EquipmentItemRepository : Repository<EquipmentItem>, IEquipmentItemRepository
    {
        private readonly SkiCenterDbContext _context;

        public EquipmentItemRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a specific ItemId 
        /// </summary>
        /// <param name="articleNumber"></param>
        public EquipmentItem GetByBarcode(string articleNumber)
        {
            if (string.IsNullOrWhiteSpace(articleNumber)) return null;

            return _context.Set<EquipmentItem>()
                .Include(ei => ei.Equipment)
                .FirstOrDefault(ei => ei.ArticleNumber == articleNumber.Trim());
        }

        /// <summary>
        /// Gets a list of all individual items in the system.
        /// </summary>
        /// 
        public IEnumerable<EquipmentItem> GetAllEquipmentItems()
        {
            return _context.EquipmentItems
                .Include(ei => ei.Equipment)
                .ToList();
        }

        /// <summary>
        /// Gets all items of a equipment type that is available. 
        /// </summary>
        /// <param name="equipmentID"></param>
        public IEnumerable<EquipmentItem> GetAvailableItems(int equipmentID)
        {
            if (equipmentID <= 0)
            {
                return Enumerable.Empty<EquipmentItem>();
            }

            // Gets all items with the status Available.
            return _context.Set<EquipmentItem>()
                .Include(ei => ei.Equipment)
                .Where(ei => ei.EquipmentID == equipmentID && ei.Status == EquipmentStatus.Available)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Updates both Status and Condition of a specific article number.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="status"></param>
        /// <param name="condition"></param>
        public void UpdateStatusAndCondition(string articleNumber, EquipmentStatus status, EquipmentCondition condition)
        {
            if (string.IsNullOrWhiteSpace(articleNumber)) return;

            var item = _context.Set<EquipmentItem>()
                .FirstOrDefault(ei => ei.ArticleNumber == articleNumber.Trim());

            if (item != null)
            {
                item.Status = status;
                item.Condition = condition;
                _context.Set<EquipmentItem>().Update(item);
            }
        }
    }
}
