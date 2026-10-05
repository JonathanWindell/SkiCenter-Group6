using System.ComponentModel.DataAnnotations;

namespace EntityLayer
{
    public class AccommodationType
    {
        [Key]
        public int AccommodationTypeID { get; set; }

        // Type of apartment
        [Required]
        public string CategoryCode { get; set; }

        // Gives general description of a singular type of accomodation
        [Required]
        public string Description { get; set; }

        [Required]
        public int NumberOfBeds { get; set; }

        // Many to Many relations. One type of accomodation can have multiple accomodations. 
        public virtual ICollection<Accommodation> Accommodations { get; set; }

        // Many to Many relations. One type of accomodation has different prices on different weeks. 
        public virtual ICollection<SeasonPrice> SeasonPrices { get; set; }

        // Parameterless constructor for EF 
        public AccommodationType() { }
    }
}
