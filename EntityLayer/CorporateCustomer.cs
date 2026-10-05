using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Maps company name to DisplayName
        [NotMapped]
        public override string DisplayName => CompanyName;

        // Parameterless constructor for EF 
        public CorporateCustomer() { }

        public CorporateCustomer(string companyName, string orgNumber, string contactPerson, string address, string email, string phoneNumber) : base(address, email, phoneNumber)
        {
            CompanyName = companyName;
            OrganisationNumber = orgNumber;
            ContactPerson = contactPerson;
            IsApproved = false;
            CreditLimit = 0;
            DiscountRate = 0;
        }
    }
}
