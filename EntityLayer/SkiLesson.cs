using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EntityLayer
{
    public class SkiLesson
    {
        [Key]
        public int SkiLessonID { get; set; }

        // Green, Blue, Red, Black, Private
        [Required]
        public string Level { get; set; }

        public int MaxParticipants { get; set; }

        [Required]
        public int DurationHours { get; set; }

        [Required]
        public decimal Price { get; set; }

        // Parameterless constructor for EF 
        public SkiLesson() { }
    }
}
