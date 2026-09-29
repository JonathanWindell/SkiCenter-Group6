using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EntityLayer
{
    public class EquipmentItem
    {
        [Key]
        public int EquipmentItemID { get; set; }

        // Status for item, ex. Available, Broken, etc. 
        [Required]
        public string Status { get; set; }

        // Describes current condition of item.
        [Required]
        public string Condition { get; set; }

        // Price per day for item.
        [Required]
        public decimal PricePerDay { get; set; }

        // Size of item.
        [Required]
        public string Size { get; set; }

        // Relation to Rental. Many to Many relation
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        // Parameterless constructor for EF 
        public EquipmentItem() { }

        public EquipmentItem(string status, string condition, decimal pricePerDay, string size)
        {
            Status = status;
            Condition = condition;
            PricePerDay = pricePerDay;
            Size = size;
        }
    }
}
