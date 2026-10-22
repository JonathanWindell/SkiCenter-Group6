using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class Rental
    {
        // Primary key for Rental
        [Key]
        public int RentalID { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; }

        public string Status { get; set; }

        // Total price for all items in the rental. Added to the booking's total amount.
        [Required]
        public decimal TotalPrice { get; set; }

        // True when staff have verified the customer's driver's license. Required for snowmobile rentals.
        [Required]
        public bool LicenseVerified { get; set; }

        // Relation to Booking. Foreign Key. Null for rentals not connected to a booking.
        [ForeignKey("Booking")]
        public int? BookingID { get; set; }
        public virtual Booking? Booking { get; set; }

        // Parameterless constructor for EF
        public Rental() { }

        // Relation to RentalItems. Many to Many relation
        public virtual ICollection<RentalItem> RentalItems { get; set; } = new List<RentalItem>();

        public Rental(DateTime startDate, DateTime endDate, DateTime returnDate, string status, decimal sum)
        {
            StartDate = startDate;
            EndDate = endDate;
            ReturnDate = returnDate;
            Status = status;
            TotalPrice = sum;
        }
    }
}
