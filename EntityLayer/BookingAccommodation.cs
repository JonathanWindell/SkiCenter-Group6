using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Text;

namespace EntityLayer
{
    public class BookingAccommodation 
    {
        [Key]
        public int BookingAccommodationID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Booking")]
        public int BookingID { get; set; }
        public virtual Booking Booking { get; set; }

        // Relation to Customer. Foreign Key
        [ForeignKey("Accommodation")]
        public int AccommodationID { get; set; }
        public virtual Accommodation Accommodation { get; set; }

        // Parameterless constructor for EF 
        public BookingAccommodation() { }

        public BookingAccommodation(DateTime startDate, DateTime endDate, decimal discountRate, decimal discountAmount, decimal finalPrice)
        {
            StartDate = startDate;
            EndDate = endDate;
            DiscountRate = discountRate;
            DiscountAmount = discountAmount;
            FinalPrice = finalPrice;
        }
    }
}
