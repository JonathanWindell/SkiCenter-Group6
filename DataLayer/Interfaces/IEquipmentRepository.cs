using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        IEnumerable<Equipment> GetByCategory(string category);
    }
}
