using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class SkiLesson
    {
        [Key]
        public int SkiLessonID { get; set; }

        // Green, Blue, Red, Black, Private
        [Required]
        public string Level { get; set; }

        [Required]
        public string Days { get; set; }

        public int MaxParticipants { get; set; }

        [Required]
        public decimal Price { get; set; }

        // Parameterless constructor for EF 
        public SkiLesson() { }

        public SkiLesson(string level, string days, int maxParticipants, decimal price)
        {
            Level = level;
            Days = days;
            MaxParticipants = maxParticipants;
            Price = price;
        }

        public virtual ICollection<SkiLessonSession> Sessions { get; set; } = new List<SkiLessonSession>();
    }
}
