using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public abstract class Customer
    {
        // Primary key for Customer
        [Key]
        public int CustomerID { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; private set; }

        // One to Many relation between Customer & Booking
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Can be uses to write out name for bookings etc. 
        [NotMapped]
        public abstract string DisplayName { get; }

        // Parameterless constructor for EF 
        public Customer() { }

        public Customer(string address, string email, string phoneNumber)
        {
            Address = address;
            Email = email;
            PhoneNumber = phoneNumber;
        }
    }
}
