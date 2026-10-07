using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public enum BookingStatus
    {
        Preliminary = 0,
        Confirmed = 1,
        Cancelled = 2,
        Finished = 3
    }

    public enum CheckInStatus
    {
        NotCheckedIn = 0,
        CheckedIn = 1,
        CheckedOut = 2
    }

    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        // Date when booking was made. 
        [Required]
        public DateTime BookingDate { get; set; }

        // Sets standard status to preliminary
        [Required]
        public BookingStatus BookingStatus { get; set; }

        public CheckInStatus CheckInStatus { get; set; } = CheckInStatus.NotCheckedIn;

        // Check if CancellationInsurance is Active
        [Required]
        public bool HasCancellationInsurance { get; set; }

        // How calculated?
        [Required]
        public decimal TotalAmount { get; set; }

        // Parameterless constructor for EF 
        public Booking() { }

        /// <summary>
        /// Given booking is central with Many to Many relations collections are used to create necessary connection.
        /// </summary>
        public virtual ICollection<BookingAccommodation> Accommodations { get; set; } = new List<BookingAccommodation>();

        public virtual ICollection<BookingMeetingRoom> MeetingRooms { get; set; } = new List<BookingMeetingRoom>();

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        public virtual ICollection<SkiLessonBooking> SkiLessons { get; set; } = new List<SkiLessonBooking>();

        // Relation to Customer. Foreign Key
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }
        public virtual Customer Customer { get; set; }
    }
}
