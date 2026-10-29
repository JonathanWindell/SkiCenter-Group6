using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    /// <summary>
    /// Calculates occupancy and sales statistics per month, compared with the same months the year before.
    /// Only MarketingManager and SystemAdmin may view statistics.
    /// </summary>
    public class StatisticsController
    {
        #region Fields and constructor

        // Longest period that can be shown at once
        public const int MaxMonths = 24;

        private readonly IUnitOfWork _unitOfWork;

        public StatisticsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets all accommodation types that have accommodations, for the type filter.
        /// </summary>
        public IEnumerable<AccommodationType> GetAccommodationTypes()
        {
            return _unitOfWork.Accommodation.GetAllAccommodations()
                .Select(a => a.AccommodationType)
                .Where(type => type != null)
                .DistinctBy(type => type.AccommodationTypeID)
                .OrderBy(type => type.CategoryCode)
                .ToList();
        }

        /// <summary>
        /// Gets occupancy and sales per month for the period and for the same months the year before.
        /// </summary>
        /// <param name="fromMonth">Any date in the first month.</param>
        /// <param name="toMonth">Any date in the last month.</param>
        /// <param name="accommodationTypeId">Optional accommodation type filter. Null includes all types.</param>
        /// <returns>Whether it succeeded, a message for the user, and the report.</returns>
        public (bool success, string message, StatisticsReport? report) GetStatistics(
            DateTime fromMonth, DateTime toMonth, int? accommodationTypeId = null)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanViewStatistics(user.Role))
            {
                return (false, "Endast marknadschef och systemadministratör har tillgång till statistik.", null);
            }

            DateTime from = new DateTime(fromMonth.Year, fromMonth.Month, 1);
            DateTime to = new DateTime(toMonth.Year, toMonth.Month, 1);

            if (to < from)
            {
                return (false, "Slutmånad kan inte vara före startmånad.", null);
            }

            List<DateTime> months = GetMonths(from, to);

            if (months.Count > MaxMonths)
            {
                return (false, $"Välj högst {MaxMonths} månader åt gången.", null);
            }

            List<Accommodation> accommodations = _unitOfWork.Accommodation.GetAllAccommodations().ToList();

            int availableUnits = accommodationTypeId.HasValue
                ? accommodations.Count(a => a.AccommodationTypeID == accommodationTypeId.Value)
                : accommodations.Count;

            string typeName = accommodationTypeId.HasValue
                ? accommodations
                    .Select(a => a.AccommodationType)
                    .FirstOrDefault(type => type != null && type.AccommodationTypeID == accommodationTypeId.Value) is AccommodationType type
                        ? $"{type.CategoryCode} – {type.Description}"
                        : "Okänd boendetyp"
                : "Alla boendetyper";

            List<DateTime> previousMonths = months.Select(m => m.AddYears(-1)).ToList();

            IEnumerable<Booking> currentBookings = _unitOfWork.Booking.GetBookingsForStatistics(from, to.AddMonths(1));
            IEnumerable<Booking> previousBookings = _unitOfWork.Booking.GetBookingsForStatistics(from.AddYears(-1), to.AddMonths(1).AddYears(-1));

            List<MonthlyStatistic> current = CalculateMonthlyStatistics(currentBookings, months, availableUnits, accommodationTypeId);
            List<MonthlyStatistic> previous = CalculateMonthlyStatistics(previousBookings, previousMonths, availableUnits, accommodationTypeId);

            var report = new StatisticsReport
            {
                FromMonth = from,
                ToMonth = to,
                AccommodationTypeID = accommodationTypeId,
                AccommodationTypeName = typeName,
                CurrentPeriod = current,
                PreviousPeriod = previous,
                CurrentTotals = PeriodSummary.FromMonths(current),
                PreviousTotals = PeriodSummary.FromMonths(previous)
            };

            return (true, $"Statistik för {months.Count} månad(er).", report);
        }

        #endregion

        #region Calculation

        /// <summary>
        /// Calculates booked nights, number of bookings and sales (excl. VAT) per month.
        /// Accommodation sales are spread over the booked nights. Equipment, ski school and other
        /// articles are spread over the days they were used. Cancelled bookings are not counted.
        /// </summary>
        /// <param name="bookings">Bookings to include.</param>
        /// <param name="months">First day of each month to calculate.</param>
        /// <param name="availableUnits">Number of accommodations, used for available nights.</param>
        /// <param name="accommodationTypeId">Optional accommodation type filter.</param>
        public static List<MonthlyStatistic> CalculateMonthlyStatistics(
            IEnumerable<Booking> bookings, IReadOnlyList<DateTime> months, int availableUnits, int? accommodationTypeId)
        {
            Dictionary<DateTime, MonthlyStatistic> stats = months.ToDictionary(
                month => month,
                month => new MonthlyStatistic
                {
                    Month = month,
                    AvailableNights = availableUnits * DateTime.DaysInMonth(month.Year, month.Month)
                });

            foreach (Booking booking in bookings.Where(b => b.BookingStatus != BookingStatus.Cancelled))
            {
                List<BookingAccommodation> stays = booking.Accommodations
                    .Where(ba => !accommodationTypeId.HasValue || ba.Accommodation?.AccommodationTypeID == accommodationTypeId.Value)
                    .ToList();

                if (stays.Count == 0)
                {
                    continue;
                }

                // Booked nights. The checkout day is not a night.
                foreach (DateTime night in stays.SelectMany(GetNights))
                {
                    if (stats.TryGetValue(FirstOfMonth(night), out MonthlyStatistic? month))
                    {
                        month.BookedNights++;
                    }
                }

                // A booking counts in the month of check-in
                if (stats.TryGetValue(FirstOfMonth(stays.Min(ba => ba.StartDate)), out MonthlyStatistic? checkInMonth))
                {
                    checkInMonth.BookingCount++;
                }

                BookingInvoice invoice = ReportController.BuildBookingInvoice(booking);

                // Accommodation sales are spread evenly over all the booking's nights,
                // and only the nights of the filtered accommodation type are counted.
                int allNights = booking.Accommodations.Sum(ba => GetNights(ba).Count());
                decimal accommodationSales = invoice.Lines.Where(l => l.ArticleType == "Boende").Sum(l => l.AmountExclVat);

                if (allNights > 0 && accommodationSales != 0)
                {
                    decimal perNight = accommodationSales / allNights;

                    foreach (DateTime night in stays.SelectMany(GetNights))
                    {
                        if (stats.TryGetValue(FirstOfMonth(night), out MonthlyStatistic? month))
                        {
                            month.AccommodationSales += perNight;
                        }
                    }
                }

                // Other articles are spread over the days they were used
                foreach (InvoiceLine line in invoice.Lines.Where(l => l.ArticleType != "Boende"))
                {
                    DateTime start = line.ServiceStartDate.Date;
                    DateTime end = line.ServiceEndDate.Date < start ? start : line.ServiceEndDate.Date;
                    int days = (end - start).Days + 1;
                    decimal perDay = line.AmountExclVat / days;

                    for (DateTime day = start; day <= end; day = day.AddDays(1))
                    {
                        if (!stats.TryGetValue(FirstOfMonth(day), out MonthlyStatistic? month))
                        {
                            continue;
                        }

                        switch (line.ArticleType)
                        {
                            case "Utrustning":
                                month.EquipmentSales += perDay;
                                break;
                            case "Skidskola":
                                month.SkiSchoolSales += perDay;
                                break;
                            default:
                                month.OtherSales += perDay;
                                break;
                        }
                    }
                }
            }

            // Round the sales once all bookings are added, so no öre are lost along the way
            foreach (MonthlyStatistic month in stats.Values)
            {
                month.AccommodationSales = Math.Round(month.AccommodationSales, 2, MidpointRounding.AwayFromZero);
                month.EquipmentSales = Math.Round(month.EquipmentSales, 2, MidpointRounding.AwayFromZero);
                month.SkiSchoolSales = Math.Round(month.SkiSchoolSales, 2, MidpointRounding.AwayFromZero);
                month.OtherSales = Math.Round(month.OtherSales, 2, MidpointRounding.AwayFromZero);
            }

            return stats.Values.OrderBy(m => m.Month).ToList();
        }

        #endregion

        #region Helpers

        private static IEnumerable<DateTime> GetNights(BookingAccommodation stay)
        {
            for (DateTime night = stay.StartDate.Date; night < stay.EndDate.Date; night = night.AddDays(1))
            {
                yield return night;
            }
        }

        private static List<DateTime> GetMonths(DateTime from, DateTime to)
        {
            var months = new List<DateTime>();

            for (DateTime month = from; month <= to; month = month.AddMonths(1))
            {
                months.Add(month);
            }

            return months;
        }

        private static DateTime FirstOfMonth(DateTime date) => new DateTime(date.Year, date.Month, 1);

        #endregion
    }
}
