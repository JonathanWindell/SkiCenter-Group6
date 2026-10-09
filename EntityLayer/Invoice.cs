using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer
{
    public class Invoice
    {
        [Key]
        public int InvoiceID { get; set; }

        // Date for creation of invoice.
        [Required]
        public DateTime Date { get; set; }

        // Last day for invoice payment.
        [Required]
        public DateTime DueDate { get; set; }

        // Invoice amount excluding moms.
        [Required]
        public decimal AmountExcl { get; set; }

        // The current moms.
        [Required]
        public decimal Moms { get; set; }

        // Status of invoice
        [Required]
        public string Status { get; set; }

        // Parameterless constructor for EF 
        public Invoice() { }

        public Invoice(DateTime date, DateTime dueDate, decimal amountExcl, decimal moms, string status)
        {
            Date = date;
            DueDate = dueDate;
            AmountExcl = amountExcl;
            Moms = moms;
            Status = status;
        }

        // Relation to Booking. Foreign Key
        [ForeignKey("Booking")]
        public int BookingID;
        public virtual Booking Booking { get; set; }

    }
}
