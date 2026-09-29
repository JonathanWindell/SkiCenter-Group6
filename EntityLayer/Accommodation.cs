using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EntityLayer
{
    public class Accommodation
    {
        [Key]
        public int AccommodationID { get; set; }

        // Specific number of unit. Apartment 101 etc. 
        [Required]
        public string UnitName{ get; set; }

        // Describes type of apartments, lodges etc. 
        [Required]
        public string Category { get; set; }

        // Specific description of the accommodation. 
        [Required]
        public string Description { get; set; }

        // Number of beds per apartment
        [Required]
        public int NumberOfBeds { get; set; }

        // Size of apartment
        [Required]
        public string Status { get; set; }

        // Parameterless constructor for EF 
        public Accommodation() { }

        public Accommodation(string unitName, string category, string description, int numberOfBeds, string status)
        {
            UnitName = unitName;
            Category = category;
            Description = description;
            NumberOfBeds = numberOfBeds;
            Status = status;
        }
    }
}
