using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class Customer
    {
        // Primary key for Customer
        [Key]
        public int CustomerID { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; private set; }

        // One to Many relation between Customer & Booking
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Parameterless constructor for EF 
        public Customer() { } 

        public Customer(string firstName, string lastName, string address, string email, string phoneNumber)
        {
            FirstName = firstName;
            LastName = lastName;
            Address = address;
            Email = email;
            PhoneNumber = phoneNumber;
        }
    }
}
