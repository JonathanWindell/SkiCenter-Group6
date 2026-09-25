using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
