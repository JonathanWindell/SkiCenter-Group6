using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EntityLayer
{
    public class BookingMeetingRoom
    {
        public int BookingMeetingID { get; set; }

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
    }
}
