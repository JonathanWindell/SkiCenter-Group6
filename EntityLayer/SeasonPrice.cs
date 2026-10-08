using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    /// <summary>
    /// A price for one accommodation type, price type and week.
    /// Prices are never overwritten. A price change adds a new row, and the row with the
    /// latest ValidFrom is the current price. Older rows are kept as price history.
    /// </summary>
    public class SeasonPrice
    {
        [Key]
        public int SeasonPriceID { get; set; }

        // How the price is charged, e.g. "vecka", "dygn", "fre-sön/dygn" or "sön-fre/dygn"
        [Required]
        public string PriceType { get; set; }

        // Week number in year
        [Required]
        public int WeekNumber { get; set; }

        // Price per accommodation
        [Required]
        public decimal Price { get; set; }

        // Date and time from which this price applies
        [Required]
        public DateTime ValidFrom { get; set; }

        // Relation to Staff. Foreign Key to the staff member who registered the price.
        // Null for prices created before history was tracked.
        [ForeignKey("ChangedBy")]
        public int? ChangedByStaffID { get; set; }
        public virtual Staff? ChangedBy { get; set; }

        // Relation to AccommodationType. Foreign Key
        [Required]
        [ForeignKey("AccommodationType")]
        public int AccommodationTypeID { get; set; }
        public virtual AccommodationType AccommodationType { get; set; }

        // Parameterless constructor for EF
        public SeasonPrice() { }

        public SeasonPrice(int accommodationTypeID, string priceType, int weekNumber, decimal price, DateTime validFrom, int? changedByStaffID)
        {
            AccommodationTypeID = accommodationTypeID;
            PriceType = priceType;
            WeekNumber = weekNumber;
            Price = price;
            ValidFrom = validFrom;
            ChangedByStaffID = changedByStaffID;
        }
    }
}
