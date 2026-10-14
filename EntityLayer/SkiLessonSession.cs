using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class SkiLessonSession
    {
        [Key]
        public int SkiLessonSessionID { get; set; }

        [Required]
        public int WeekNumber { get; set; }

        // Morning or Afternoon
        [Required]
        public string TimeSlot { get; set; }

        // Relation to SkiLesson. Foreign Key
        [Required]
        [ForeignKey("SkiLesson")]
        public int SkiLessonID { get; set; }
        public virtual SkiLesson SkiLesson { get; set; }

        // Lists number of participants
        public virtual ICollection<SkiLessonBooking> Participants { get; set; } = new List<SkiLessonBooking>();

        // Parameterless constructor for EF 
        public SkiLessonSession() { }

        public SkiLessonSession(int weekNumber, string timeSlot, int skiLessonId)
        {
            WeekNumber = weekNumber;
            TimeSlot = timeSlot;
            SkiLessonID = skiLessonId;
        }
    }
}
