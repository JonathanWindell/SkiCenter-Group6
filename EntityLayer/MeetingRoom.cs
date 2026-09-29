using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
