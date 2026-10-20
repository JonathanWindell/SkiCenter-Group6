namespace PresentationLayer.Models
{
    /// <summary>
    /// One row in the statistics table: a month in the selected period compared with the same month the year before.
    /// </summary>
    public class StatisticsRow
    {
        public string Month { get; set; } = string.Empty;
        public string Occupancy { get; set; } = string.Empty;
        public string PreviousOccupancy { get; set; } = string.Empty;
        public int BookedNights { get; set; }
        public int BookingCount { get; set; }
        public string AccommodationSales { get; set; } = string.Empty;
        public string EquipmentSales { get; set; } = string.Empty;
        public string SkiSchoolSales { get; set; } = string.Empty;
        public string TotalSales { get; set; } = string.Empty;
        public string PreviousSales { get; set; } = string.Empty;
        public string SalesChange { get; set; } = string.Empty;
    }
}
