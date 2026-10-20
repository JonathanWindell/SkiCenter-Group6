namespace BusinessLayer.Models
{
    /// <summary>
    /// Occupancy and sales statistics for a period, compared with the same months the year before.
    /// </summary>
    public class StatisticsReport
    {
        public DateTime FromMonth { get; set; }
        public DateTime ToMonth { get; set; }

        // Null when all accommodation types are included
        public int? AccommodationTypeID { get; set; }
        public string AccommodationTypeName { get; set; } = "Alla boendetyper";

        public List<MonthlyStatistic> CurrentPeriod { get; set; } = new();
        public List<MonthlyStatistic> PreviousPeriod { get; set; } = new();

        public PeriodSummary CurrentTotals { get; set; } = new();
        public PeriodSummary PreviousTotals { get; set; } = new();

        // Change in occupancy in percentage points, e.g. 0.05 for +5 percentage points
        public decimal OccupancyChange => CurrentTotals.OccupancyRate - PreviousTotals.OccupancyRate;

        // Change in percent compared with the previous period. Null when the previous period has no data.
        public decimal? SalesChangePercent => PercentChange(CurrentTotals.TotalSales, PreviousTotals.TotalSales);
        public decimal? BookingCountChangePercent => PercentChange(CurrentTotals.BookingCount, PreviousTotals.BookingCount);
        public decimal? BookedNightsChangePercent => PercentChange(CurrentTotals.BookedNights, PreviousTotals.BookedNights);

        public static decimal? PercentChange(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return null;
            }

            return Math.Round((current - previous) / previous, 4, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Statistics for one month. Sales are excluding VAT.
    /// </summary>
    public class MonthlyStatistic
    {
        public DateTime Month { get; set; }
        public int AvailableNights { get; set; }
        public int BookedNights { get; set; }
        public decimal OccupancyRate => AvailableNights == 0 ? 0m : Math.Round((decimal)BookedNights / AvailableNights, 4, MidpointRounding.AwayFromZero);

        // Bookings with check-in during the month
        public int BookingCount { get; set; }

        public decimal AccommodationSales { get; set; }
        public decimal EquipmentSales { get; set; }
        public decimal SkiSchoolSales { get; set; }
        public decimal OtherSales { get; set; }
        public decimal TotalSales => AccommodationSales + EquipmentSales + SkiSchoolSales + OtherSales;
    }

    /// <summary>
    /// Totals for a whole period.
    /// </summary>
    public class PeriodSummary
    {
        public int AvailableNights { get; set; }
        public int BookedNights { get; set; }
        public decimal OccupancyRate => AvailableNights == 0 ? 0m : Math.Round((decimal)BookedNights / AvailableNights, 4, MidpointRounding.AwayFromZero);
        public int BookingCount { get; set; }
        public decimal AccommodationSales { get; set; }
        public decimal EquipmentSales { get; set; }
        public decimal SkiSchoolSales { get; set; }
        public decimal OtherSales { get; set; }
        public decimal TotalSales => AccommodationSales + EquipmentSales + SkiSchoolSales + OtherSales;

        public static PeriodSummary FromMonths(IEnumerable<MonthlyStatistic> months)
        {
            List<MonthlyStatistic> list = months.ToList();

            return new PeriodSummary
            {
                AvailableNights = list.Sum(m => m.AvailableNights),
                BookedNights = list.Sum(m => m.BookedNights),
                BookingCount = list.Sum(m => m.BookingCount),
                AccommodationSales = list.Sum(m => m.AccommodationSales),
                EquipmentSales = list.Sum(m => m.EquipmentSales),
                SkiSchoolSales = list.Sum(m => m.SkiSchoolSales),
                OtherSales = list.Sum(m => m.OtherSales)
            };
        }
    }
}
