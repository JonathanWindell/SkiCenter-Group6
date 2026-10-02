using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class PrivateCustomer : Customer
    {
        [Required]
        public decimal CreditLimit { get; set; }

        // Parameterless constructor for EF 
        public PrivateCustomer() { }

        public PrivateCustomer(string firstName, string lastName, string address, string email, string phoneNumber, decimal creditLimit) : base(firstName, lastName, address, email, phoneNumber)
        {
            CreditLimit = creditLimit;
        }
    }
}
