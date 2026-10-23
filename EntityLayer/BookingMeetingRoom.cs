using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace EntityLayer
{
    public class BookingMeetingRoom
    {
        public int BookingMeetingRoomID { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Parameterless constructor for EF 
        public BookingMeetingRoom() { }

        // Relation to Booking. Foreign Key
        [ForeignKey("Booking")]
        public int BookingID { get; set; }
        public virtual Booking booking { get; set; }

        // Relation to Meeting room. Foreign Key
        [ForeignKey("MeetingRoom")]
        public int MeetingRoomID { get; set; }
        public virtual MeetingRoom meetingRoom { get; set; }

        // Needed to calculate price for WPF. 
        [NotMapped]
        public decimal TotalPrice
        {
            get
            {
                if (meetingRoom?.SeasonPrices == null) return 0m;

                int week = ISOWeek.GetWeekOfYear(StartDate);
                int days = Math.Max(1, (EndDate.Date - StartDate.Date).Days);

                var priceObj = meetingRoom.SeasonPrices.FirstOrDefault(p => p.PriceType == "dygn" && p.WeekNumber == week);
                return (priceObj?.Price ?? 0m) * days;
            }
        }
    }
}
