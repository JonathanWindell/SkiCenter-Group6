using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class CorporateCustomer : Customer
    {
        [Required]
        public string CompanyName { get; set; }
        public string OrganisationNumber { get; set; }
        [Required]
        public string ContactPerson { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal DiscountRate { get; set; }
        public bool IsApproved { get; set; } // Decided by marketing manager

        // Parameterless constructor for EF 
        public CorporateCustomer() { }

        public CorporateCustomer(string firstName, string lastName, string address, string email, string phoneNumber, string orgNumber, string contactPerson, string companyName) : base(firstName, lastName, address, email, phoneNumber)
        {
            CompanyName = companyName;
            OrganisationNumber = orgNumber;
            ContactPerson = contactPerson;
            IsApproved = false; // Standard value (Pending)
            CreditLimit = 0;
            DiscountRate = 0;
        }
    }
}
