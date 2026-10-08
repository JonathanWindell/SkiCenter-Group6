using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        // Description for the equipment.
        [Required]
        public string Description { get; set; }

        // Describes type of equipment, skis, scooters etc. 
        [Required]
        public string Category { get; set; }

        public string DisplayName => $"{Category} - {Description}";

        // Many to Many relations
        public virtual ICollection<EquipmentItem> EquipmentItems { get; set; }
        public virtual ICollection<EquipmentPriceMatrix> Prices { get; set; }

        // Parameterless constructor for EF 
        public Equipment() { }

        public Equipment(string description, string category)
        {
            Description = description;
            Category = category;
        }
    }
}
