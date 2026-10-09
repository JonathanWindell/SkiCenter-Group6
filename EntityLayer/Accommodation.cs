using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public enum accommodationStatus
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
        public accommodationStatus Status { get; set; }

        [Required]
        [ForeignKey("AccommodationType")]
        public int AccommodationTypeID { get; set; }
        public virtual AccommodationType AccommodationType { get; set; }

        // Parameterless constructor for EF 
        public Accommodation() { }

        public Accommodation(string accommodationNumber, accommodationStatus status)
        {
            AccommodationNumber = accommodationNumber;
            Status = status;
        }
    }
}
