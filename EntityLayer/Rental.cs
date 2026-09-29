using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EntityLayer
{
    public class Rental
    {
        // Primary key for Rental
        [Key]
        public int RentalID { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        public DateOnly ReturnDate { get; set; }

        public string Status { get; set; }

        public decimal Sum { get; set; }

        // Parameterless constructor for EF 
        public Rental() { }

        // Relation to EquipmentItems. Many to Many relation
        public virtual ICollection<EquipmentItem> EquipmentItems { get; set; } = new List<EquipmentItem>();

        public Rental(DateOnly startDate, DateOnly endDate, DateOnly returnDate, string status, decimal sum)
        {
            StartDate = startDate;
            EndDate = endDate;
            ReturnDate = returnDate;
            Status = status;
            Sum = sum;
        }

    }
}
