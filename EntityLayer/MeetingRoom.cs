using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public enum MeetingRoomStatus
    {
        Available,
        Rented,
        Maintenance,
    }

    public class MeetingRoom
    {
        // Primary key for Meeting room
        [Key]
        public int MeetingRoomID { get; set; }

        [Required]
        public string MeetingRoomNumber { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public MeetingRoomStatus Status { get; set; }

        // Parameterless constructor for EF 
        public MeetingRoom() { }

        public MeetingRoom(string meetingRoomNumber, int capacity, string description, MeetingRoomStatus status)
        {
            MeetingRoomNumber = meetingRoomNumber;
            Capacity = capacity;
            Description = description;
            Status = status;
        }

        public virtual ICollection<BookingMeetingRoom> BookingMeetingRooms { get; set; } = new List<BookingMeetingRoom>();

        public virtual ICollection<MeetingRoomSeasonPrice> SeasonPrices { get; set; } = new List<MeetingRoomSeasonPrice>();
    }
}
