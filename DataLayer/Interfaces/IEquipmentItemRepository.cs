using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IEquipmentItemRepository : IRepository<EquipmentItem>
    {
        EquipmentItem GetByBarcode(string articleNumber);
        IEnumerable<EquipmentItem> GetAvailableItems(int equipmentItemID);
        void UpdateEquipmentItem(string articleNumber, int equipmentID, string size, EquipmentStatus status, EquipmentCondition condition);
        IEnumerable<EquipmentItem> GetAllEquipmentItems();
        void UpdateStatusAndCondition(string articleNumber, EquipmentStatus status, EquipmentCondition condition);
    }
}