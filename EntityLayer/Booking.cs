using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        //Date when booking was made. 
        [Required]
        public DateTime BookingDate { get; set; }

        // Sets standard status to preliminary
        [Required]
        public string Status { get; set; } // "Preliminary", "Confirmed", "Cancelled"

        // Check if CancellationInsurance is Active
        [Required]
        public bool HasCancellationInsurance { get; set; }

        // How calculated?
        [Required]
        public decimal TotalAmount { get; set; }

        // Parameterless constructor for EF 
        public Booking() { }

        public Booking(DateTime bookingDate, string status, bool hasCancellationInsurance, decimal totalAmount)
        {
            BookingDate = bookingDate;
            Status = status;
            HasCancellationInsurance = hasCancellationInsurance;
            TotalAmount = totalAmount;
        }

        /// <summary>
        /// Given booking is central with Many to Many relations collections are used to create necessary connection.
        /// </summary>
        public virtual ICollection<BookingAccommodation> Accommodations { get; set; } = new List<BookingAccommodation>();

        public virtual ICollection<MeetingRoom> MeetingRooms { get; set; } = new List<MeetingRoom>();

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        public virtual ICollection<SkiLessonBooking> SkiLessons { get; set; } = new List<SkiLessonBooking>();


        // Relation to Customer. Foreign Key
        [ForeignKey("Customer")]
        public int CustomerID { get; private set; }
        public virtual Customer Customer { get; set; }


    }
}
