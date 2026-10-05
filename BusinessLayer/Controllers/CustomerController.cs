using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class CustomerController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all registered customers with all associated booking data.
        /// </summary>
        public IEnumerable<Customer> GetAllCustomers()
        {
            return _unitOfWork.Customer.GetAllCustomers();
        }

        /// <summary>
        /// Searches for a specific customer. 
        /// </summary>
        public Customer SearchCustomer(string searchItem)
        {
            if (string.IsNullOrWhiteSpace(searchItem)) return null;
            return _unitOfWork.Customer.SearchCustomer(searchItem);
        }

        /// <summary>
        /// Gets a single customer with all bookinghistory and accommodation data. 
        /// </summary>
        public Customer GetCustomerHistory(int customerId)
        {
            if (customerId <= 0) return null;
            return _unitOfWork.Customer.GetCustomerHistory(customerId);
        }

        /// <summary>
        /// Gets all corporate customers that has not yet been approved by marketing manager.
        /// </summary>
        public IEnumerable<CorporateCustomer> GetPendingCorporateCustomers()
        {
            return _unitOfWork.Customer.GetPendingCorporateCustomer();
        }


        /// <summary>
        /// Validates and creates a new PrivateCustomer with a default credit limit of 12000 kr.
        /// </summary>
        public bool RegisterPrivateCustomer(string firstName, string lastName, string address, string email, string phoneNumber)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                !IsValidEmail(email))
            {
                return false;
            }

            // Checks for already existing Customer in database
            if (_unitOfWork.Customer.CustomerExists(email.Trim(), phoneNumber.Trim()))
            {
                return false;
            }

            // Sets default creditlimit to 12000
            var newCustomer = new PrivateCustomer(
                firstName.Trim(),
                lastName.Trim(),
                address.Trim(),
                email.Trim(),
                phoneNumber.Trim(),
                creditLimit: 12000m
            );

            _unitOfWork.Customer.Add(newCustomer);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Validates and creates a new CorporateCustomer, with default state: pending
        /// </summary>
        public bool RegisterCorporateCustomer(
            string companyName,
            string orgNumber,
            string contactPerson,
            string address,
            string email,
            string phoneNumber)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(companyName) ||
                string.IsNullOrWhiteSpace(orgNumber) ||
                string.IsNullOrWhiteSpace(contactPerson) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                !IsValidEmail(email))
            {
                return false;
            }

            // Checks for already existing CorporateCustomer in database
            if (_unitOfWork.Customer.CustomerExists(email.Trim(), phoneNumber.Trim()))
            {
                return false;
            }

            // Create new Corporate Customer, default IsApproved = false, CreditLimit = 0 and DiscountRate = 0
            var newCorpCustomer = new CorporateCustomer(
                companyName: companyName.Trim(),
                firstName: companyName.Trim(),
                lastName: "",
                address: address.Trim(),
                email: email.Trim(),
                phoneNumber: phoneNumber.Trim(),
                orgNumber: orgNumber.Trim(),
                contactPerson: contactPerson.Trim()
            )
            {
                IsApproved = false,
                CreditLimit = 0m,
                DiscountRate = 0m
            };

            _unitOfWork.Customer.Add(newCorpCustomer);
            return _unitOfWork.Complete() > 0;
        }


        /// <summary>
        /// For marketing manager. Approves CorporateCustomer and sets creditlimit and discountrate.
        /// </summary>
        public bool ApproveCorporateCustomer(int customerId, decimal creditLimit, decimal discountRate)
        {
            // Validation
            if (customerId <= 0 || creditLimit < 0 || discountRate < 0 || discountRate > 100)
            {
                return false;
            }

            var customer = _unitOfWork.Customer.GetByID(customerId);
            if (customer is CorporateCustomer corpCustomer)
            {
                corpCustomer.IsApproved = true;
                corpCustomer.CreditLimit = creditLimit;
                corpCustomer.DiscountRate = discountRate;

                _unitOfWork.Customer.Update(corpCustomer);
                return _unitOfWork.Complete() > 0;
            }

            return false;
        }

        /// <summary>
        /// Updates existing data with new input for existing customer.
        /// </summary>
        public bool UpdateCustomerDetails(Customer customer)
        {
            // Validation
            if (customer == null || customer.CustomerID <= 0) return false;

            if (string.IsNullOrWhiteSpace(customer.Address) ||
                !IsValidEmail(customer.Email))
            {
                return false;
            }

            _unitOfWork.Customer.Update(customer);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Removes a customer if there is no active bookings.
        /// </summary>
        public bool DeleteCustomer(int customerId)
        {
            var customer = _unitOfWork.Customer.GetCustomerHistory(customerId);
            if (customer == null) return false;

            // Logic for protecting against customer removal when there is a preliminary or confirmed booking
            bool hasActiveBookings = customer.Bookings.Any(b => b.Status == "Preliminary" || b.Status == "Confirmed");
            if (hasActiveBookings)
            {
                return false;
            }

            _unitOfWork.Customer.Delete(customer);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Checks if customer qualifies for recurring booking discount (8%).
        /// Eligble for PrivateCustomers that has made a booking the last 365 days.
        /// </summary>
        public bool IsEligibleForReturningDiscount(int customerId)
        {
            var customer = _unitOfWork.Customer.GetCustomerHistory(customerId);
            if (customer == null || !(customer is PrivateCustomer))
            {
                return false;
            }

            var oneYearAgo = DateTime.Now.AddDays(-365);

            // Checks if last booking ended in 365 days
            return customer.Bookings.Any(b =>
                (b.Status == "Confirmed" || b.Status == "Completed") &&
                b.Accommodations.Any(ba => ba.EndDate >= oneYearAgo));
        }

        /// <summary>
        /// Checks if email contains @ and if its ends or start with @.
        /// </summary>
        /// <param name="email"></param>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return email.Contains('@') && !email.StartsWith("@") && !email.EndsWith('@');
        }
    }
}
