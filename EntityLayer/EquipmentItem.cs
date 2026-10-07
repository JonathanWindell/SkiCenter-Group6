using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public enum EquipmentStatus
    {
        Available = 0,
        Rented = 1,
        Damaged = 2,
        Maintenance = 3
    }

    public enum EquipmentCondition
    {
        New = 0,
        BarelyUsed = 1,
        Worn = 2
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
        public EquipmentStatus Status { get; set; }

        // Describes current condition of item.
        [Required]
        [AllowedValues("New", "BarelyUsed", "Worn", ErrorMessage = "Not allowed value")]
        public EquipmentCondition Condition { get; set; }

        // Size of item.
        [Required]
        public string Size { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Equipment")]
        public int EquipmentID { get; set; }
        public virtual Equipment Equipment { get; set; }

        public EquipmentItem() { }
    }
}
