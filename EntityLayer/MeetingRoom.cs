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
        public int SeasonPriceID { get; set; }

        [Required]
        public string AccommodationType { get; set; }

        [Required]
        public int WeekNumber { get; set; }

        [Required]
        public decimal Price { get; set; }

        // Parameterless constructor for EF 
        public MeetingRoom() { }

        public MeetingRoom(string accomodationType, int weekNumber, decimal price)
        {
            AccommodationType = accomodationType;
            WeekNumber = weekNumber;
            Price = price;
        }
    }
}
