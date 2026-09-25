using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
        public int staffID { get; set; }

        [Required]
        public string firstName { get; set; }

        [Required]
        public string lastName { get; set; }

        [Required]
        public string email { get; set; }

        [Required]
        [MinLength(10)]
        public string password { get; set; }

        public staffRole role { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Parameterless constructor for EF
        public Staff() { }

        public Staff(string firstName, string lastName, string email, string password)
        {
            firstName = firstName;
            lastName = lastName;
            email = email;
            password = password;
        }
    }
}
