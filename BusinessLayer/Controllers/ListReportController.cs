using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;
using System.Globalization;

namespace BusinessLayer.Controllers
{
    /// <summary>
    /// Gathers the data for printable reports: statistics, customer register, equipment register and price lists.
    /// Each report includes key messages that summarize what the report shows.
    /// Only MarketingManager and SystemAdmin may create these reports.
    /// </summary>
    public class ListReportController
    {
        private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

        private readonly IUnitOfWork _unitOfWork;

        public ListReportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Creates the statistics report from statistics already shown in the statistics view.
        /// </summary>
        public (bool success, string message, StatisticsPdfReport? report) CreateStatisticsReport(StatisticsReport statistics, bool compareWithPreviousYear)
        {
            if (!TryGetAllowedUser(out Staff? user, out string error))
            {
                return (false, error, null);
            }

            if (statistics == null)
            {
                return (false, "Visa statistik innan rapporten skapas.", null);
            }

            var report = new StatisticsPdfReport
            {
                Title = "Statistikrapport",
                CreatedAt = DateTime.Now,
                CreatedBy = FormatUser(user!),
                Statistics = statistics,
                CompareWithPreviousYear = compareWithPreviousYear,
                KeyMessages = BuildStatisticsMessages(statistics, compareWithPreviousYear)
            };

            return (true, "Statistikrapporten är skapad.", report);
        }

        /// <summary>
        /// Creates the customer register with every private and corporate customer.
        /// </summary>
        public (bool success, string message, CustomerRegisterReport? report) CreateCustomerRegister()
        {
            if (!TryGetAllowedUser(out Staff? user, out string error))
            {
                return (false, error, null);
            }

            List<Customer> customers = _unitOfWork.Customer.GetAllCustomers().ToList();

            var report = new CustomerRegisterReport
            {
                Title = "Kundregister",
                CreatedAt = DateTime.Now,
                CreatedBy = FormatUser(user!),
                Customers = customers
                    .OrderBy(c => c is CorporateCustomer)
                    .ThenBy(c => c.DisplayName)
                    .Select(CreateCustomerRow)
                    .ToList()
            };

            report.PrivateCustomerCount = customers.Count(c => c is PrivateCustomer);
            report.CorporateCustomerCount = customers.Count(c => c is CorporateCustomer);
            report.PendingCorporateCount = customers.OfType<CorporateCustomer>().Count(c => !c.IsApproved);
            report.KeyMessages = BuildCustomerMessages(report);

            return (true, "Kundregistret är skapat.", report);
        }

        /// <summary>
        /// Creates the ski equipment register with every item and a status summary per category.
        /// </summary>
        public (bool success, string message, EquipmentRegisterReport? report) CreateEquipmentRegister()
        {
            if (!TryGetAllowedUser(out Staff? user, out string error))
            {
                return (false, error, null);
            }

            List<EquipmentItem> items = _unitOfWork.EquipmentItem.GetAllEquipmentItems().ToList();

            var report = new EquipmentRegisterReport
            {
                Title = "Skidutrustningsregister",
                CreatedAt = DateTime.Now,
                CreatedBy = FormatUser(user!),
                TotalItems = items.Count,
                AvailableItems = items.Count(i => i.Status == EquipmentStatus.Available),
                RentedItems = items.Count(i => i.Status == EquipmentStatus.Rented),
                NeedsActionItems = items.Count(i => i.Status == EquipmentStatus.Damaged || i.Status == EquipmentStatus.Maintenance),
                Categories = items
                    .GroupBy(i => i.Equipment?.Category ?? "Okänd")
                    .OrderBy(g => g.Key)
                    .Select(g => new EquipmentCategorySummary
                    {
                        Category = g.Key,
                        Available = g.Count(i => i.Status == EquipmentStatus.Available),
                        Rented = g.Count(i => i.Status == EquipmentStatus.Rented),
                        Damaged = g.Count(i => i.Status == EquipmentStatus.Damaged),
                        Maintenance = g.Count(i => i.Status == EquipmentStatus.Maintenance)
                    })
                    .ToList(),
                Items = items
                    .OrderBy(i => i.Equipment?.Category)
                    .ThenBy(i => i.Equipment?.Description)
                    .ThenBy(i => i.ArticleNumber)
                    .Select(i => new EquipmentRegisterRow
                    {
                        Category = i.Equipment?.Category ?? "Okänd",
                        Model = i.Equipment?.Description ?? string.Empty,
                        ArticleNumber = i.ArticleNumber,
                        Size = i.Size,
                        Status = StatusText(i.Status),
                        Condition = ConditionText(i.Condition)
                    })
                    .ToList()
            };

            report.KeyMessages = BuildEquipmentMessages(report);

            return (true, "Skidutrustningsregistret är skapat.", report);
        }

        /// <summary>
        /// Creates price lists for accommodation (current season prices), equipment rental and ski school.
        /// </summary>
        public (bool success, string message, PriceListReport? report) CreatePriceList()
        {
            if (!TryGetAllowedUser(out Staff? user, out string error))
            {
                return (false, error, null);
            }

            var report = new PriceListReport
            {
                Title = "Prislistor",
                CreatedAt = DateTime.Now,
                CreatedBy = FormatUser(user!),
                AccommodationPrices = BuildAccommodationPriceLists(_unitOfWork.SeasonPrice.GetCurrentPrices()),
                EquipmentPrices = _unitOfWork.Equipment.GetAllEquipmentWithPrices()
                    .Where(e => e.Prices != null && e.Prices.Any())
                    .Select(e => new EquipmentPriceRow
                    {
                        Category = e.Category,
                        Model = e.Description,
                        PricesByDays = e.Prices
                            .GroupBy(p => p.Days)
                            .OrderBy(g => g.Key)
                            .ToDictionary(g => g.Key, g => (decimal)g.First().TotalAmount)
                    })
                    .ToList(),
                SkiLessonPrices = _unitOfWork.SkiLesson.GetLessonsWithSessions()
                    .OrderBy(l => l.Price)
                    .Select(l => new SkiLessonPriceRow
                    {
                        Level = l.Level,
                        Days = l.Days,
                        MaxParticipants = l.MaxParticipants,
                        Price = l.Price
                    })
                    .ToList()
            };

            report.KeyMessages = BuildPriceListMessages(report);

            return (true, "Prislistorna är skapade.", report);
        }

        /// <summary>
        /// Groups current season prices into one price list per accommodation type, with one row per week.
        /// The weekly price ("vecka") is used to find the lowest price and the high season weeks.
        /// </summary>
        public static List<AccommodationPriceList> BuildAccommodationPriceLists(IEnumerable<SeasonPrice> prices)
        {
            return prices
                .GroupBy(p => p.AccommodationType != null
                    ? $"{p.AccommodationType.CategoryCode} – {p.AccommodationType.Description}"
                    : $"Boendetyp {p.AccommodationTypeID}")
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    List<SeasonPrice> weekly = g.Where(p => p.PriceType == "vecka").ToList();
                    decimal? highest = weekly.Count > 0 ? weekly.Max(p => p.Price) : null;

                    return new AccommodationPriceList
                    {
                        AccommodationType = g.Key,
                        PriceTypes = g.Select(p => p.PriceType).Distinct().OrderBy(t => t == "vecka" ? 0 : 1).ThenBy(t => t).ToList(),
                        Weeks = g
                            .GroupBy(p => p.WeekNumber)
                            .OrderBy(w => SeasonOrder(w.Key))
                            .Select(w => new WeekPriceRow
                            {
                                WeekNumber = w.Key,
                                Prices = w.GroupBy(p => p.PriceType).ToDictionary(t => t.Key, t => t.First().Price)
                            })
                            .ToList(),
                        LowestWeekPrice = weekly.Count > 0 ? weekly.Min(p => p.Price) : null,
                        HighestWeekPrice = highest,
                        HighSeasonWeeks = highest.HasValue
                            ? weekly.Where(p => p.Price == highest.Value).Select(p => p.WeekNumber).OrderBy(SeasonOrder).ToList()
                            : new List<int>()
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Orders weeks in season order: the winter season runs from week 44 over new year to the spring.
        /// </summary>
        private static int SeasonOrder(int week) => week >= 40 ? week - 40 : week + 13;

        public static List<string> BuildStatisticsMessages(StatisticsReport statistics, bool compareWithPreviousYear)
        {
            PeriodSummary current = statistics.CurrentTotals;
            var messages = new List<string>
            {
                $"Beläggningen för {statistics.AccommodationTypeName.ToLower(Swedish)} var {current.OccupancyRate.ToString("P1", Swedish)} " +
                $"({current.BookedNights.ToString("N0", Swedish)} av {current.AvailableNights.ToString("N0", Swedish)} möjliga nätter).",
                $"Försäljningen uppgick till {current.TotalSales.ToString("N0", Swedish)} kr exkl. moms från {current.BookingCount} bokning(ar)."
            };

            MonthlyStatistic? best = statistics.CurrentPeriod.Where(m => m.TotalSales > 0).OrderByDescending(m => m.TotalSales).FirstOrDefault();
            MonthlyStatistic? weakest = statistics.CurrentPeriod.OrderBy(m => m.TotalSales).FirstOrDefault();

            if (best != null)
            {
                messages.Add($"Starkaste månaden var {MonthName(best.Month)} med {best.TotalSales.ToString("N0", Swedish)} kr" +
                    (weakest != null && weakest.Month != best.Month ? $", svagaste var {MonthName(weakest.Month)} med {weakest.TotalSales.ToString("N0", Swedish)} kr." : "."));
            }

            if (compareWithPreviousYear)
            {
                messages.Add(statistics.SalesChangePercent.HasValue
                    ? $"Jämfört med föregående år {(statistics.SalesChangePercent.Value >= 0 ? "ökade" : "minskade")} försäljningen med " +
                      $"{Math.Abs(statistics.SalesChangePercent.Value).ToString("P0", Swedish)} och beläggningen ändrades med " +
                      $"{(statistics.OccupancyChange * 100).ToString("+0.0;-0.0;0.0", Swedish)} procentenheter."
                    : "Det finns ingen försäljning föregående år att jämföra med.");
            }

            return messages;
        }

        public static List<string> BuildCustomerMessages(CustomerRegisterReport report)
        {
            int total = report.PrivateCustomerCount + report.CorporateCustomerCount;
            var messages = new List<string>
            {
                $"Registret innehåller {total} kunder: {report.PrivateCustomerCount} privatkunder och {report.CorporateCustomerCount} företagskunder."
            };

            messages.Add(report.PendingCorporateCount > 0
                ? $"{report.PendingCorporateCount} företagskund(er) väntar på godkännande av marknadschef."
                : "Alla företagskunder är godkända.");

            int withoutBookings = report.Customers.Count(c => c.BookingCount == 0);
            if (withoutBookings > 0)
            {
                messages.Add($"{withoutBookings} kund(er) har ännu inte gjort någon bokning.");
            }

            return messages;
        }

        public static List<string> BuildEquipmentMessages(EquipmentRegisterReport report)
        {
            var messages = new List<string>
            {
                $"Registret innehåller {report.TotalItems.ToString("N0", Swedish)} artiklar i {report.Categories.Count} kategorier.",
                $"{report.AvailableShare.ToString("P1", Swedish)} är tillgängliga för uthyrning och {report.RentedItems} är uthyrda just nu."
            };

            messages.Add(report.NeedsActionItems > 0
                ? $"{report.NeedsActionItems} artikel(ar) är skadade eller på service och behöver åtgärdas."
                : "Ingen utrustning är skadad eller på service.");

            EquipmentCategorySummary? largest = report.Categories.OrderByDescending(c => c.Total).FirstOrDefault();
            if (largest != null)
            {
                messages.Add($"Största kategorin är {largest.Category} med {largest.Total.ToString("N0", Swedish)} artiklar.");
            }

            return messages;
        }

        public static List<string> BuildPriceListMessages(PriceListReport report)
        {
            var messages = new List<string>();

            foreach (AccommodationPriceList list in report.AccommodationPrices.Where(l => l.LowestWeekPrice.HasValue))
            {
                messages.Add($"{list.AccommodationType}: veckopris från {list.LowestWeekPrice!.Value.ToString("N0", Swedish)} kr " +
                    $"till {list.HighestWeekPrice!.Value.ToString("N0", Swedish)} kr. Högsäsong vecka {string.Join(", ", list.HighSeasonWeeks)}.");
            }

            if (report.EquipmentPrices.Count > 0)
            {
                messages.Add($"Prislistan för uthyrning omfattar {report.EquipmentPrices.Count} typer av utrustning.");
            }

            if (report.SkiLessonPrices.Count > 0)
            {
                messages.Add($"Skidskolan har {report.SkiLessonPrices.Count} kurser från {report.SkiLessonPrices.Min(l => l.Price).ToString("N0", Swedish)} kr.");
            }

            if (messages.Count == 0)
            {
                messages.Add("Det finns inga priser registrerade.");
            }

            return messages;
        }

        private static CustomerRegisterRow CreateCustomerRow(Customer customer)
        {
            var row = new CustomerRegisterRow
            {
                CustomerID = customer.CustomerID,
                Name = customer.DisplayName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Address = customer.Address,
                CreditLimit = customer.CreditLimit,
                BookingCount = customer.Bookings?.Count ?? 0
            };

            if (customer is CorporateCustomer corporate)
            {
                row.CustomerType = "Företagskund";
                row.OrganisationNumber = corporate.OrganisationNumber;
                row.ContactPerson = corporate.ContactPerson;
                row.DiscountRate = corporate.DiscountRate;
                row.IsApproved = corporate.IsApproved;
            }
            else
            {
                row.CustomerType = "Privatkund";
            }

            return row;
        }

        private static bool TryGetAllowedUser(out Staff? user, out string error)
        {
            user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanViewStatistics(user.Role))
            {
                error = "Endast marknadschef och systemadministratör kan skapa rapporter.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string FormatUser(Staff user) => $"{user.FirstName} {user.LastName}";

        private static string MonthName(DateTime month) => month.ToString("MMMM yyyy", Swedish);

        private static string StatusText(EquipmentStatus status) => status switch
        {
            EquipmentStatus.Available => "Tillgänglig",
            EquipmentStatus.Rented => "Uthyrd",
            EquipmentStatus.Damaged => "Skadad",
            EquipmentStatus.Maintenance => "Service",
            _ => status.ToString()
        };

        private static string ConditionText(EquipmentCondition condition) => condition switch
        {
            EquipmentCondition.New => "Ny",
            EquipmentCondition.BarelyUsed => "Lite använd",
            EquipmentCondition.Worn => "Sliten",
            _ => condition.ToString()
        };
    }
}
