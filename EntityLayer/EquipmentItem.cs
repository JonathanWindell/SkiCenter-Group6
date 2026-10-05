using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public enum equipmentStatus
    {
        Available,
        Rented,
        Damaged,
        Maintenance,
        Archived
    }

    public enum equipmentCondition
    {
        New,
        BarelyUsed,
        Worn
    }

    public class EquipmentItem
    {
        [Key]
        public int EquipmentItemID { get; set; }

        // Unique number for each post in database. 
        public string ArticleNumber { get; set; }

        // Status for item, ex. Available, Broken, etc. 
        [Required]
        [AllowedValues("Available", "Rented", "Damaged", "Maintenance", ErrorMessage = "Not allowed value")]
        public equipmentStatus Status { get; set; }

        // Describes current condition of item.
        [Required]
        [AllowedValues("New", "BarelyUsed", "Worn", ErrorMessage = "Not allowed value")]
        public equipmentCondition Condition { get; set; }

        // Size of item.
        [Required]
        public string Size { get; set; }

        // Relation to Rental. Many to Many relation
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        // Relation to Equipment. Foreign Key
        [ForeignKey("Equipment")]
        public int EquipmentID { get; set; }
        public virtual Equipment Equipment { get; set; }

        // Parameterless constructor for EF 
        public EquipmentItem() { }

        public EquipmentItem(equipmentStatus status, equipmentCondition condition, string size, string articleNumber)
        {
            Status = status;
            Condition = condition;
            Size = size;
            ArticleNumber = articleNumber;
        }
    }
}
