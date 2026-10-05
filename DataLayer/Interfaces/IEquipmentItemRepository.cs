using EntityLayer;
using System.Runtime.CompilerServices;

namespace DataLayer.Interfaces
{
    public interface IEquipmentItemRepository : IRepository<EquipmentItem>
    {
        EquipmentItem GetByBarcode(string articleNumber);
        IEnumerable<EquipmentItem> GetAvailableItems(int equipmentItemID);
        void UpdateStatusAndCondition(string articleNumber, equipmentStatus status, equipmentCondition condition);
    }
}