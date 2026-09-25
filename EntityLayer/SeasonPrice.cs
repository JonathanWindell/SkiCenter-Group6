using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EntityLayer
{
    public class SeasonPrice
    {
        [Key]
        public int SeasonPriceID { get; set; }

        // Type of apartment. Nr 1 or 2
        [Required]
        public string AccommodationType { get; set; }

        // Week number in year
        [Required]
        public int WeekNumber { get; set; }

        // Price per apartment
        [Required]
        public decimal Price { get; set; }

        // Parameterless constructor for EF 
        public SeasonPrice() { }
    }
}
