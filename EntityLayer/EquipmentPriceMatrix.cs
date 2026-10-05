using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class EquipmentPriceMatrix
    {
        [Key]
        public int EquipmentPriceMatrixID { get; set; }

<<<<<<< HEAD
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
=======
        // Amount of days.
        [Required]
        public int Days { get; set; }

        // Total amount.
        [Required]
        public decimal TotalAmount { get; set; }




        // Relation to EquipmentItem. Foreign Key
        [ForeignKey("EquipmentItem")]
        public int EquipmentItemID { get; set; }
        public virtual EquipmentItem EquipmentItem { get; set; }

        // Parameterless constructor for EF 
        public EquipmentPriceMatrix() { }

        public EquipmentPriceMatrix(int days, decimal totalAmount)
        {
            Days = days;
            TotalAmount = totalAmount;
        }
    }
}
>>>>>>> origin/johan-branch
