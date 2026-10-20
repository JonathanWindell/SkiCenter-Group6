namespace BusinessLayer.Models
{
    /// <summary>
    /// Shared information for every printable report: title, who created it and the report's main messages.
    /// </summary>
    public abstract class ReportBase
    {
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // The report's key messages, shown in a summary box at the top
        public List<string> KeyMessages { get; set; } = new();
    }

    /// <summary>
    /// Statistics report: the statistics view's content as a printable report.
    /// </summary>
    public class StatisticsPdfReport : ReportBase
    {
        public StatisticsReport Statistics { get; set; } = new();
        public bool CompareWithPreviousYear { get; set; }
    }

    /// <summary>
    /// The customer register with all private and corporate customers.
    /// </summary>
    public class CustomerRegisterReport : ReportBase
    {
        public int PrivateCustomerCount { get; set; }
        public int CorporateCustomerCount { get; set; }
        public int PendingCorporateCount { get; set; }
        public List<CustomerRegisterRow> Customers { get; set; } = new();
    }

    public class CustomerRegisterRow
    {
        public int CustomerID { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganisationNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal? DiscountRate { get; set; }
        public bool? IsApproved { get; set; }
        public int BookingCount { get; set; }
    }

    /// <summary>
    /// The ski equipment register with every item and a status summary per category.
    /// </summary>
    public class EquipmentRegisterReport : ReportBase
    {
        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int RentedItems { get; set; }
        public int NeedsActionItems { get; set; }
        public decimal AvailableShare => TotalItems == 0 ? 0m : Math.Round((decimal)AvailableItems / TotalItems, 4, MidpointRounding.AwayFromZero);
        public List<EquipmentCategorySummary> Categories { get; set; } = new();
        public List<EquipmentRegisterRow> Items { get; set; } = new();
    }

    public class EquipmentCategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int Available { get; set; }
        public int Rented { get; set; }
        public int Damaged { get; set; }
        public int Maintenance { get; set; }
        public int Total => Available + Rented + Damaged + Maintenance;
    }

    public class EquipmentRegisterRow
    {
        public string Category { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ArticleNumber { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Price lists for accommodation, equipment rental and ski school.
    /// </summary>
    public class PriceListReport : ReportBase
    {
        public List<AccommodationPriceList> AccommodationPrices { get; set; } = new();
        public List<EquipmentPriceRow> EquipmentPrices { get; set; } = new();
        public List<SkiLessonPriceRow> SkiLessonPrices { get; set; } = new();
    }

    /// <summary>
    /// Current season prices for one accommodation type: one row per week, one column per price type.
    /// </summary>
    public class AccommodationPriceList
    {
        public string AccommodationType { get; set; } = string.Empty;
        public List<string> PriceTypes { get; set; } = new();
        public List<WeekPriceRow> Weeks { get; set; } = new();
        public decimal? LowestWeekPrice { get; set; }
        public decimal? HighestWeekPrice { get; set; }

        // Weeks with the highest weekly price
        public List<int> HighSeasonWeeks { get; set; } = new();
    }

    public class WeekPriceRow
    {
        public int WeekNumber { get; set; }

        // Price per price type, e.g. "vecka" → 4 500
        public Dictionary<string, decimal> Prices { get; set; } = new();
    }

    public class EquipmentPriceRow
    {
        public string Category { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;

        // Price per number of days, e.g. 3 → 330
        public Dictionary<int, decimal> PricesByDays { get; set; } = new();
    }

    public class SkiLessonPriceRow
    {
        public string Level { get; set; } = string.Empty;
        public string Days { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public decimal Price { get; set; }
    }
}
