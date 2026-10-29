using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class CustomerController
    {
        #region Fields and constructor

        private readonly IUnitOfWork _unitOfWork;

        public CustomerController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Customer queries

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
        public IEnumerable<CorporateCustomer> GetAllCorporateCustomers()
        {
            return _unitOfWork.Customer.GetAllCorporateCustomers();
        }

        #endregion

        #region Registration

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

        #endregion

        #region Updates and deletion

        /// <summary>
        /// Updates terms and approval status för a specific CorporateCustomer.
        /// </summary>
        public bool UpdateCorporateCustomer(int customerId, bool isApproved, decimal creditLimit, decimal discountPercent)
        {
            if (customerId <= 0 || creditLimit < 0 || discountPercent < 0 || discountPercent > 100)
            {
                return false;
            }

            var customer = _unitOfWork.Customer.GetByID(customerId) as CorporateCustomer;
            if (customer == null) return false;

            bool wasPreviouslyApproved = customer.IsApproved;

            decimal rateAsDecimal = discountPercent > 0m ? discountPercent / 100m : 0m;

            customer.IsApproved = isApproved;
            customer.CreditLimit = creditLimit;
            customer.DiscountRate = rateAsDecimal;

            _unitOfWork.Customer.Update(customer);

            // Confirm preliminary bookings if Customer is approved
            if (!wasPreviouslyApproved && isApproved)
            {
                var preliminaryBookings = _unitOfWork.Booking.GetBookingsByCustomer(customerId)
                    .Where(b => b.BookingStatus == BookingStatus.Preliminary)
                    .ToList();

                foreach (var b in preliminaryBookings)
                {
                    b.BookingStatus = BookingStatus.Confirmed;
                }
            }

            return _unitOfWork.Complete() > 0;
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
            bool hasActiveBookings = customer.Bookings.Any(b =>
            b.BookingStatus == BookingStatus.Preliminary ||
            b.BookingStatus == BookingStatus.Confirmed);
            if (hasActiveBookings)
            {
                return false;
            }

            _unitOfWork.Customer.Delete(customer);
            return _unitOfWork.Complete() > 0;
        }

        #endregion

        #region Discounts and validation

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
                (b.BookingStatus == BookingStatus.Finished) &&
                b.Accommodations.Any(ba => ba.EndDate >= oneYearAgo && ba.EndDate <= DateTime.Now));
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

        #endregion

        #region GDPR

        /// <summary>
        /// Anonymise customers personal information. Ensures that historical and financial data is kept intact. 
        /// </summary>
        /// <param name="customerId"></param>
        public (bool success, string message) AnonymizeCustomer(int customerId)
        {
            if (customerId <= 0)
            {
                return (false, "Ogiltigt kund-ID.");
            }

            var customer = _unitOfWork.Customer.GetByID(customerId);
            if (customer == null)
            {
                return (false, "Kunde inte hitta kunden i systemet.");
            }

            // Overwrite the fields that both type of customers share
            customer.Email = $"anonym_{Guid.NewGuid().ToString().Substring(0, 8)}@raderad.local";
            customer.PhoneNumber = "000-0000000";
            customer.Address = "Raderad";

            // Overwrite the fields that create DisplayName
            if (customer is PrivateCustomer privateCustomer)
            {
                privateCustomer.FirstName = "Anonymiserad";
                privateCustomer.LastName = "Kund";
            }
            else if (customer is CorporateCustomer corpCustomer)
            {
                corpCustomer.CompanyName = "Anonymiserat Företag";
                corpCustomer.OrganisationNumber = "000000-0000";
                corpCustomer.ContactPerson = "Raderad";
            }

            _unitOfWork.Customer.Update(customer);

            if (_unitOfWork.Complete() > 0)
            {
                return (true, "Kundens uppgifter har anonymiserats permanent i enlighet med GDPR.");
            }

            return (false, "Ett databasfel uppstod när kunden skulle anonymiseras.");
        }

        #endregion
    }
}
