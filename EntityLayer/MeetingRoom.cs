using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class MeetingRoom
    {
        // Primary key for Meeting room
        [Key]
        public int MeetingRoomID { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public string WeekDay { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; }

        // Parameterless constructor for EF 
        public MeetingRoom() { }

        // Relation to Booking. Many to Many Relation. 
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public MeetingRoom(int capacity, string weekDay, string description, decimal price, string status)
        {
            Capacity = capacity;
            WeekDay = weekDay;
            Description = description;
            Price = price;
            Status = status;
            Status = status;
        }
    }
}
