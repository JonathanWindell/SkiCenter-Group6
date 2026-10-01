using System;
using System.Collections.Generic;
using System.Text;
using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        IEnumerable<Customer> GetAllCustomers();
        
        void UpdateCustomer(Customer customer);
        
        Customer GetCustomerHistory(int customerID);

        Customer SearchCustomer(string searchItem);

        bool CustomerExists(string email, string phoneNumber);

        IEnumerable<CorporateCustomer> GetPendingCorporateCustomer();       
    }
}
