using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class EquipmentPriceMatrix
    {
        [Key]
        public int EquipmentPriceMatrixID { get; set; }

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