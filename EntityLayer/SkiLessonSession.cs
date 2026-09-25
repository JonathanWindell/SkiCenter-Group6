using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EntityLayer
{
    public class SkiLessonSession
    {
        [Key]
        public int SkiLessonSessionID { get; set; }

        [Required] 
        public int WeekNumber { get; set; }

        // Monday - Wednesday or Thursday to Friday
        [Required]
        public string Days { get; set; }

        // Morning or Afternoon
        [Required]
        public string TimeSlot { get; set; } 

        // Relation to Customer. Foreign Key
        [ForeignKey("Customer")]
        public int SkiLessonID { get; set; }
        public virtual SkiLesson SkiLesson { get; set; }

        // Lists number of participants
        public virtual ICollection<SkiLessonBooking> Participants { get; set; } = new List<SkiLessonBooking>();

        // Parameterless constructor for EF 
        public SkiLessonSession() { }
    }
}
