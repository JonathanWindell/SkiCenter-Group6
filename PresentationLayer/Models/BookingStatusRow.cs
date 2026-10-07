using System;

namespace PresentationLayer.Models
{
    // Display data for one row in the booking overview.
    public class BookingStatusRow
    {
        public int BookingID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        // Nullable dates allow bookings without accommodation.
        public DateTime? ArrivalDate { get; set; }

        public DateTime? DepartureDate { get; set; }

        public string BookingStatus { get; set; } = string.Empty;

        // Supplied separately; the UI does not calculate payment status.
        public string PaymentStatus { get; set; } = string.Empty;
    }
}