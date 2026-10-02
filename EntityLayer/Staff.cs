using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public enum staffRole
    {
        SkiShop,
        MarketingManager,
        BookingAdmin,
        SystemAdmin
    }

    public class Staff
    {
        // Primary key for Staff
        [Key]
        public int StaffID { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        [MinLength(10)]
        public string Password { get; set; }

        public staffRole Role { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Parameterless constructor for EF
        public Staff() { }

        public Staff(string firstName, string lastName, string email, string password, staffRole role)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Password = password;
            Role = role;
        }
    }
}
