using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EntityLayer
{
    public class Accomodation
    {
        [Key]
        public int AccommodationID { get; set; }

        // Describes type of apartments, lodges etc. 
        [Required]
        public string Category { get; set; }

        // Specific number of unit. Apartment 101 etc. 
        [Required]
        public string UnitNumber { get; set; }

        // Number of beds per apartment
        [Required]
        public int NumberOfBeds { get; set; }

        // Size of apartment
        [Required]
        public int SizeM2 { get; set; }

        // Parameterless constructor for EF 
        public Accomodation() { }
    }
}
