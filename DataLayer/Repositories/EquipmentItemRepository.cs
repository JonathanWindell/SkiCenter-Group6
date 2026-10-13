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
                    .ThenInclude(e => e.Prices)
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
        /// Gets all available items, optionally for one category, with their equipment and price list.
        /// </summary>
        /// <param name="category">Optional category filter, e.g. "Skoter".</param>
        public IEnumerable<EquipmentItem> GetAvailableItemsWithPrices(string? category)
        {
            IQueryable<EquipmentItem> items = _context.Set<EquipmentItem>()
                .Include(ei => ei.Equipment)
                    .ThenInclude(e => e.Prices)
                .Where(ei => ei.Status == EquipmentStatus.Available);

            if (!string.IsNullOrWhiteSpace(category))
            {
                string trimmedCategory = category.Trim();
                items = items.Where(ei => ei.Equipment.Category == trimmedCategory);
            }

            return items
                .OrderBy(ei => ei.Equipment.Category)
                .ThenBy(ei => ei.Equipment.Description)
                .ThenBy(ei => ei.ArticleNumber)
                .AsSplitQuery()
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets specific items by their IDs, with equipment and price list. The items are tracked so they can be updated.
        /// </summary>
        /// <param name="equipmentItemIDs"></param>
        public IEnumerable<EquipmentItem> GetItemsByIds(IEnumerable<int> equipmentItemIDs)
        {
            List<int> ids = equipmentItemIDs?.Distinct().ToList() ?? new List<int>();

            if (ids.Count == 0)
            {
                return Enumerable.Empty<EquipmentItem>();
            }

            return _context.Set<EquipmentItem>()
                .Include(ei => ei.Equipment)
                    .ThenInclude(e => e.Prices)
                .Where(ei => ids.Contains(ei.EquipmentItemID))
                .AsSplitQuery()
                .ToList();
        }

        /// <summary>
        /// Updates an item with new properties.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="equipmentID"></param>
        /// <param name="size"></param>
        /// <param name="status"></param>
        /// <param name="condition"></param>
        public void UpdateEquipmentItem(string articleNumber, int equipmentID, string size, EquipmentStatus status, EquipmentCondition condition)
        {
            if (string.IsNullOrWhiteSpace(articleNumber)) return;

            var item = _context.Set<EquipmentItem>()
                .FirstOrDefault(ei => ei.ArticleNumber == articleNumber.Trim());

            if (item != null)
            {   
                item.EquipmentID = equipmentID;
                item.Size = size;
                item.Status = status;
                item.Condition = condition;

                _context.Set<EquipmentItem>().Update(item);
            }
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
