using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class BookingAccomodation
    {
        [Key]
        public int BookingAccommodationID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Booking")]
        public int BookingID { get; set; }
        public virtual Booking Booking { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Accomodaton")]
        public int AccommodationID { get; set; }
        public virtual Accommodation Accommodation { get; set; }

        // Parameterless constructor for EF 
        public BookingAccomodation() { }
    }
}
