using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IStaffRepository
    {
        Staff GetStaffByEmailAndPassword(string email, string password);
    }
}
