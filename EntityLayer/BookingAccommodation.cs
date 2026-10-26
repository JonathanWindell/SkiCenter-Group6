using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class BookingAccommodation
    {
        [Key]
        public int BookingAccommodationID { get; set; }

        // Start date for booked accommodation
        public DateTime StartDate { get; set; }

        // End date for booked accommodation 
        public DateTime EndDate { get; set; }

        // Relation to Customer. Foreign Key 
        [ForeignKey("Booking")]
        public int BookingID { get; set; }
        public virtual Booking Booking { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Accommodation")]
        public int AccommodationID { get; set; }
        public virtual Accommodation Accommodation { get; set; }

        // Parameterless constructor for EF 
        public BookingAccommodation() { }

        public BookingAccommodation(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
