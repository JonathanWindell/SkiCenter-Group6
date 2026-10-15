using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class MeetingRoomSeasonPrice
    {
        [Key]
        public int MeetingRoomSeasonPriceID { get; set; }

        [Required]
        public int WeekNumber { get; set; }

        [Required]
        public string PriceType { get; set; } // "vecka", "dygn", "tim"

        [Required]
        public decimal Price { get; set; }

        [Required]
        [ForeignKey("MeetingRoom")]
        public int MeetingRoomID { get; set; }
        public virtual MeetingRoom MeetingRoom { get; set; }

        public MeetingRoomSeasonPrice() { }
    }
}
