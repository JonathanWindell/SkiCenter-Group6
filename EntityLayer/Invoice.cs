using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text;

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

        // Invoice amount including moms.
        [Required]
        public decimal AmountIncl { get; set; }

        // Status of invoice
        [Required]
        public string Status { get; set; }

        // Parameterless constructor for EF 
        public Invoice() { }

        public Invoice(DateTime date, DateTime dueDate, decimal amountExcl, decimal moms, decimal amountIncl, string status)
        {
            Date = date;
            DueDate = dueDate;
            AmountExcl = amountExcl;
            Moms = moms;
            AmountIncl = amountIncl;
            Status = status;
        }
    }
}
