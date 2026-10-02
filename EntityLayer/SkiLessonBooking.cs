using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class SkiLessonBooking
    {
        [Key]
        public int SkiLessonBookingID { get; set; }

        // Relation to SkiLessonSession. Foreign Key
        [ForeignKey("SkiLessonSession")]
        public int SkiLessonSessionID { get; private set; }
        public virtual SkiLessonSession SkiLessonSession { get; set; }

        // Relation to Booking. Foreign Key
        [ForeignKey("Booking")]
        public int BookingID { get; private set; }
        public virtual Booking Booking { get; set; }


    }
}
