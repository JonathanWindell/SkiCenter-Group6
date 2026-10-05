using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class EquipmentPriceMatrix
    {
        [Key]
        public int EquipmentPriceMatrixID { get; set; }

        // Contains data on how many days equipment is rented
        [Required]
        public int Days { get; set; }

        [Required]
        public int TotalAmount { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Equipment")]
        public int EquipmentID { get; private set; }
        public virtual Equipment Equipment { get; set; }
    }
}
