using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class RentalItem
    {
        [Key]
        public int RentalItemID { get; set; }

        [Required]
        [ForeignKey("Rental")]
        public int RentalID { get; set; }
        public virtual Rental Rental { get; set; }

        [Required]
        [ForeignKey("EquipmentItem")]
        public int EquipmentItemID { get; set; }
        public virtual EquipmentItem EquipmentItem { get; set; }

        public RentalItem() { }
    }
}
