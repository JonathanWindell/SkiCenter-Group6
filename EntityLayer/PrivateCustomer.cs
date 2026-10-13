using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class PrivateCustomer : Customer
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        // Describes what should be mapped to DisplayName
        [NotMapped]
        public override string DisplayName => $"{FirstName} {LastName}";

        // Parameterless constructor for EF 
        public PrivateCustomer() 
        {
            CreditLimit = 12000;
        }

        public PrivateCustomer(string firstName, string lastName, string address, string email, string phoneNumber, decimal creditLimit) : base(address, email, phoneNumber)
        {
            FirstName = firstName;
            LastName = lastName;
            CreditLimit = 12000;
        }
    }
}
