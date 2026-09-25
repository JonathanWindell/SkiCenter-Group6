using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EntityLayer
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }
        
        //Date when booking was made. 
        public DateTime BookingDate { get; set; }

        // Sets standard status to preliminary
        public string Status { get; private set; } // "Preliminary", "Confirmed", "Cancelled"
        public bool HasCancellationInsurance { get; set; }

        // How calculated?
        public decimal TotalAmount { get; set; }

        // Parameterless constructor for EF 
        public Booking() { }

        /// <summary>
        /// Given booking is central with Many to Many relations collections are used to create necessary connection.
        /// </summary>
        public virtual ICollection<BookingAccommodation> Accommodations { get; set; } = new List<BookingAccommodation>();

        public virtual ICollection<MeetingRoom> MeetingRooms { get; set; } = new List<MeetingRoom>();

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public virtual ICollection<Rental> Rental { get; set; } = new List<Invoice>();


        // Relation to Customer. Foreign Key
        [ForeignKey("Customer")]
        public int CustomerID { get; private set; }
        public virtual Customer Customer { get; set; }


    }
}
