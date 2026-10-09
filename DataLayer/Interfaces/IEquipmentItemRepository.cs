using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IEquipmentItemRepository : IRepository<EquipmentItem>
    {
        EquipmentItem GetByBarcode(string articleNumber);
        IEnumerable<EquipmentItem> GetAvailableItems(int equipmentItemID);
        void UpdateStatusAndCondition(string articleNumber, EquipmentStatus status, EquipmentCondition condition);
        IEnumerable<EquipmentItem> GetAllEquipmentItems();
    }
}