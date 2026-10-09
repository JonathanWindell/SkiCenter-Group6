using System;

namespace PresentationLayer.Models
{
    // Holds guest details displayed in the check in/out view.
    public class GuestStayRow
    {
        public int BookingID { get; set; }
        public int BookingAccommodationID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string AccommodationName { get; set; } = string.Empty;
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string StayStatus { get; set; } = string.Empty;
    }
}