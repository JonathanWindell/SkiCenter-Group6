using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IStaffRepository : IRepository<Staff>
    {
        // Defines method GetStaffByEmailAndPassword
        Staff GetStaffByEmailAndPassword(string email, string password);

        // Defines method EmailExists
        bool EmailExists(string email);

        // Gets every staff account, active and deactivated
        IEnumerable<Staff> GetAllStaff();
    }
}
