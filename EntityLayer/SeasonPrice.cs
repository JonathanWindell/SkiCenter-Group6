using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class SeasonPrice
    {
        [Key]
        public int SeasonPriceID { get; set; }

        // Week number in year
        [Required]
        public int WeekNumber { get; set; }

        // Type of Price. Week, Weekend or Week. 
        [Required]
        public string PriceType { get; set; }

        // Price per apartment
        [Required]
        public decimal Price { get; set; }

        public int AccommodationTypeID { get; set; }
        public virtual AccommodationType AccommodationType { get; set; }

        // Parameterless constructor for EF 
        public SeasonPrice() { }

        public SeasonPrice(int weekNumber, string priceType, decimal price)
        {
            WeekNumber = weekNumber;
            PriceType = priceType;
            Price = price;
        }
    }
}
