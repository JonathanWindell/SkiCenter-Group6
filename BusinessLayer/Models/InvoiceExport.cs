namespace BusinessLayer.Models
{
    /// <summary>
    /// Invoicing data for all completed bookings in a period, ready to be exported as JSON.
    /// </summary>
    public class InvoiceExport
    {
        public DateTime ExportedAt { get; set; }
        public string ExportedBy { get; set; } = string.Empty;
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public string Currency { get; set; } = "SEK";
        public bool PricesIncludeVat { get; set; } = true;
        public int BookingCount { get; set; }
        public decimal TotalExclVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalInclVat { get; set; }
        public List<VatSummary> VatSummary { get; set; } = new();
        public List<BookingInvoice> Bookings { get; set; } = new();
    }

    /// <summary>
    /// Invoicing data for one completed booking.
    /// </summary>
    public class BookingInvoice
    {
        public int BookingID { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public InvoiceCustomer Customer { get; set; } = new();

        // Corporate customers are invoiced with VAT specified separately; private customers pay prices including VAT.
        public string InvoiceType { get; set; } = string.Empty;

        public List<InvoiceLine> Lines { get; set; } = new();
        public List<AppliedDiscount> Discounts { get; set; } = new();
        public List<VatSummary> VatSummary { get; set; } = new();
        public decimal TotalExclVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalInclVat { get; set; }

        // The total saved on the booking, so finance can see if the lines do not add up to it.
        public decimal BookingTotalAmount { get; set; }
        public bool MatchesBookingTotal { get; set; }
    }

    public class InvoiceCustomer
    {
        public int CustomerID { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganisationNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// One article on the invoice, e.g. an accommodation, a rented item or a ski lesson.
    /// </summary>
    public class InvoiceLine
    {
        public string ArticleType { get; set; } = string.Empty;
        public string ArticleNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;

        // Price before discount, including VAT
        public decimal ListPriceInclVat { get; set; }
        public decimal DiscountInclVat { get; set; }
        public decimal AmountInclVat { get; set; }
        public decimal AmountExclVat { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
    }

    public class AppliedDiscount
    {
        public string Description { get; set; } = string.Empty;
        public string AppliesTo { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal AmountInclVat { get; set; }
    }

    /// <summary>
    /// Totals for one VAT rate.
    /// </summary>
    public class VatSummary
    {
        public decimal VatRate { get; set; }
        public decimal AmountExclVat { get; set; }
        public decimal VatAmount { get; set; }
        public decimal AmountInclVat { get; set; }
    }
}
