using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public enum meetingRoomStatus
    {
        Availabe,
        Rented,
        Maintenence,
    }

    public class MeetingRoom
    {
        // Primary key for Meeting room
        [Key]
        public int MeetingRoomID { get; set; }

        [Required]
        public int MeetingRoomNumber { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public meetingRoomStatus Status { get; set; }

        // Parameterless constructor for EF 
        public MeetingRoom() { }

        // Relation to Booking. Many to Many Relation. 
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public MeetingRoom(int capacity, DateTime startDate, DateTime endDate, string description, decimal price, meetingRoomStatus status)
        {
            Capacity = capacity;
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
            Price = price;
            Status = status;
        }
    }
}
