using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    // Enum for available statuses for Equipment
    public enum EquipmentStatus
    {
        Available = 0,
        Rented = 1,
        Damaged = 2,
        Maintenance = 3
    }

    // Available conditions for Equipment
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

        // Unique number for each post in database
        [Required]
        public string ArticleNumber { get; set; }

        // Statuses for EquipmentItems.  
        [Required]
        [AllowedValues("Available", "Rented", "Damaged", "Maintenance", ErrorMessage = "Not allowed value")]
        public EquipmentStatus Status { get; set; }

        // Conditions for EquipmentItems.
        [Required]
        [AllowedValues("New", "BarelyUsed", "Worn", ErrorMessage = "Not allowed value")]
        public EquipmentCondition Condition { get; set; }

        // Size of item. Describes length of skis, boot size etc
        [Required]
        public string Size { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Equipment")]
        public int EquipmentID { get; set; }
        public virtual Equipment Equipment { get; set; }

        // Collection of EquipmentItems that are coupled to a Rental for customer. 
        public virtual ICollection<RentalItem> RentalItems { get; set; } = new List<RentalItem>();

        public EquipmentItem() { }

        // Used for WPF views. 
        [NotMapped]
        public string ConditionSwedish => Condition switch
        {
            EquipmentCondition.New => "Ny",
            EquipmentCondition.BarelyUsed => "Knappt använd",
            EquipmentCondition.Worn => "Använd",
            _ => Condition.ToString()
        };

        [NotMapped]
        public string StatusSwedish => Status switch
        {
            EquipmentStatus.Available => "Tillgänglig",
            EquipmentStatus.Rented => "Uthyrd",
            EquipmentStatus.Damaged => "Skadad",
            EquipmentStatus.Maintenance => "Under underhåll",
            _ => Status.ToString()
        };
    }
}
