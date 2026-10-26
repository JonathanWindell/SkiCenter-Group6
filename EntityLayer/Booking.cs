using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    // Enum for available statuses of a booking
    public enum BookingStatus
    {
        Preliminary = 0,
        Confirmed = 1,
        Cancelled = 2,
        Finished = 3
    }

    // Enum for available check-in statuses of a booking
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

        // Accommodation price before discount, saved when the booking is created. Used for invoicing.
        [Required]
        public decimal AccommodationListPrice { get; set; }

        // Discount rate applied to the accommodation, e.g. 0.08 for 8%.
        [Required]
        public decimal DiscountRate { get; set; }

        // Discount amount deducted from the accommodation price.
        [Required]
        public decimal DiscountAmount { get; set; }

        // Used for calculations of final price of accommodation. 
        [NotMapped]
        public decimal FinalAccommodationPrice
        {
            get
            {
                if (AccommodationListPrice > 0)
                {
                    return Math.Round(AccommodationListPrice * (1m - DiscountRate), 2, MidpointRounding.AwayFromZero);
                }

                decimal rawAccom = Accommodations?.Sum(a =>
                    a.Accommodation?.AccommodationType != null ? a.Accommodation.AccommodationType.NumberOfBeds : 0m) ?? 0m;

                return Math.Round(rawAccom * (1m - DiscountRate), 2, MidpointRounding.AwayFromZero);
            }
            private set { }
        }

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
