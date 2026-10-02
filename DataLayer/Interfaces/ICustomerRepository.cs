using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        IEnumerable<Customer> GetAllCustomers();

        Customer GetCustomerHistory(int customerID);

        Customer SearchCustomer(string searchItem);

        bool CustomerExists(string email, string phoneNumber);

        IEnumerable<CorporateCustomer> GetPendingCorporateCustomer();
    }
}
