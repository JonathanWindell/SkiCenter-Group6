using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        private readonly SkiCenterDbContext _context;

        public CustomerRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all customers with all associated booking data.
        /// </summary>
        public IEnumerable<Customer> GetAllCustomers()
        {
            return _context.Set<Customer>()
                .Include(c => c.Bookings)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Updates customer with new data.
        /// </summary>
        /// <param name="customer"></param>
        public void UpdateCustomer(Customer customer)
        {
            _context.Set<Customer>().Update(customer);
        }

        /// <summary>
        /// Gets a single customer with all bookinghistory and accommodation data. 
        /// </summary>
        /// <param name="customerID"></param>
        public Customer GetCustomerHistory(int customerID)
        {
            return _context.Set<Customer>()
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Accommodations)
                        .ThenInclude(ba => ba.Accommodation)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Invoices)
                .AsSplitQuery()
                .FirstOrDefault(c => c.CustomerID == customerID);
        }

        /// <summary>
        /// Searches for a specific customer. 
        /// </summary>
        /// <param name="searchItem"></param>
        public Customer SearchCustomer(string searchItem)
        {
            if (string.IsNullOrWhiteSpace(searchItem))
            {
                return null;
            }

            string search = searchItem.Trim();

            return _context.Set<Customer>()
                .Include(c => c.Bookings)
                .FirstOrDefault(c =>
                    c.PhoneNumber == search ||
                    c.Email == search ||
                    (c.FirstName + " " + c.LastName).Contains(search) ||
                    (c is CorporateCustomer && (((CorporateCustomer)c).OrganisationNumber == search || ((CorporateCustomer)c).CompanyName.Contains(search)))
                );
        }

        /// <summary>
        /// Checks if there is already a registered customer with email and phonenumber. 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="phoneNumber"></param>
        public bool CustomerExists(string email, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            return _context.Set<Customer>().Any(c =>
                (!string.IsNullOrEmpty(email) && c.Email == email) ||
                (!string.IsNullOrEmpty(phoneNumber) && c.PhoneNumber == phoneNumber));
        }
        /// <summary>
        /// Gets all corporate customers that has not yet been approved by marketing manager. 
        /// </summary>
        public IEnumerable<CorporateCustomer> GetPendingCorporateCustomer()
        {
            return _context.Set<CorporateCustomer>()
                .Where(cc => !cc.IsApproved)
                .AsNoTracking()
                .ToList();
        }
    }
}
