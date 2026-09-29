using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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

        // Parameterless constructor for EF 
        public Equipment() { }

        public Equipment(string description, string category)
        {
            Description = description;
            Category = category;
        }
    }
}
