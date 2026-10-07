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
        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public meetingRoomStatus Status { get; set; }

        // Parameterless constructor for EF 
        public MeetingRoom() { }

        public MeetingRoom(int capacity, string description, decimal price, meetingRoomStatus status)
        {
            Capacity = capacity;
            Description = description;
            Price = price;
            Status = status;
        }

        public virtual ICollection<BookingMeetingRoom> BookingMeetingRooms { get; set; } = new List<BookingMeetingRoom>();
    }
}
