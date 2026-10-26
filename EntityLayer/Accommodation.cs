using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    // Enum for available statuses of Accommodations
    public enum AccommodationStatus
    {
        Available = 0,
        Rented = 1,
        Damaged = 2,
        Maintenance = 3
    }

    public class Accommodation
    {
        [Key]
        public int AccommodationID { get; set; }

        // Specific number of unit. Apartment 101 etc. 
        [Required]
        public string AccommodationNumber { get; set; }

        // "Available", "Cleaning", "Maintenance"
        [Required]
        public AccommodationStatus Status { get; set; }

        [Required]
        [ForeignKey("AccommodationType")]
        public int AccommodationTypeID { get; set; }
        public virtual AccommodationType AccommodationType { get; set; }

        public virtual ICollection<BookingAccommodation> BookingAccommodations { get; set; } = new List<BookingAccommodation>();

        // Parameterless constructor for EF 
        public Accommodation() { }

        public Accommodation(string accommodationNumber, AccommodationStatus status)
        {
            AccommodationNumber = accommodationNumber;
            Status = status;
        }
    }
}
