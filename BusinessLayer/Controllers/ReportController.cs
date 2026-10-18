using BusinessLayer.Models;
using DataLayer.Interfaces;
using EntityLayer;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BusinessLayer.Controllers
{
    /// <summary>
    /// Which customers to include in an invoicing export.
    /// </summary>
    public enum InvoiceCustomerFilter
    {
        All,
        Private,
        Corporate
    }

    /// <summary>
    /// Generates invoicing data for completed bookings, for import into the financial system.
    /// Only MarketingManager and SystemAdmin may generate exports.
    /// </summary>
    public class ReportController
    {
        private readonly IUnitOfWork _unitOfWork;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Keeps Swedish characters (å, ä, ö) readable in the file
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ReportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Generates invoicing data for all completed bookings with checkout in the period.
        /// </summary>
        /// <param name="fromDate">First checkout date.</param>
        /// <param name="toDate">Last checkout date.</param>
        /// <param name="customerFilter">Which customer types to include.</param>
        /// <returns>Whether it succeeded, a message for the user, and the export.</returns>
        public (bool success, string message, InvoiceExport? export) GenerateInvoiceExport(
            DateTime fromDate, DateTime toDate, InvoiceCustomerFilter customerFilter = InvoiceCustomerFilter.All)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanExportInvoices(user.Role))
            {
                return (false, "Endast marknadschef och systemadministratör kan exportera faktureringsunderlag.", null);
            }

            if (toDate.Date < fromDate.Date)
            {
                return (false, "Slutdatum kan inte vara före startdatum.", null);
            }

            IEnumerable<Booking> bookings = _unitOfWork.Booking.GetFinishedBookingsForInvoicing(fromDate, toDate)
                .Where(b => customerFilter switch
                {
                    InvoiceCustomerFilter.Private => b.Customer is PrivateCustomer,
                    InvoiceCustomerFilter.Corporate => b.Customer is CorporateCustomer,
                    _ => true
                });

            List<BookingInvoice> invoices = bookings.Select(BuildBookingInvoice).ToList();

            var export = new InvoiceExport
            {
                ExportedAt = DateTime.Now,
                ExportedBy = $"{user.FirstName} {user.LastName} ({user.Email})",
                PeriodFrom = fromDate.Date,
                PeriodTo = toDate.Date,
                BookingCount = invoices.Count,
                Bookings = invoices,
                VatSummary = SummarizeVat(invoices.SelectMany(i => i.Lines)),
                TotalExclVat = invoices.Sum(i => i.TotalExclVat),
                TotalVat = invoices.Sum(i => i.TotalVat),
                TotalInclVat = invoices.Sum(i => i.TotalInclVat)
            };

            string message = invoices.Count == 0
                ? "Inga avslutade bokningar hittades för perioden."
                : $"{invoices.Count} avslutade bokning(ar) hittades.";

            return (true, message, export);
        }

        /// <summary>
        /// Converts an export to JSON for the financial system.
        /// </summary>
        public static string ToJson(InvoiceExport export)
        {
            return JsonSerializer.Serialize(export, JsonOptions);
        }

        /// <summary>
        /// Builds the invoice for one booking: one line per article, applied discounts and VAT per rate.
        /// </summary>
        public static BookingInvoice BuildBookingInvoice(Booking booking)
        {
            var lines = new List<InvoiceLine>();
            var discounts = new List<AppliedDiscount>();

            DateTime checkIn = booking.Accommodations.Any() ? booking.Accommodations.Min(ba => ba.StartDate).Date : booking.BookingDate.Date;
            DateTime checkOut = booking.Accommodations.Any() ? booking.Accommodations.Max(ba => ba.EndDate).Date : booking.BookingDate.Date;

            List<InvoiceLine> rentalLines = booking.Rentals.SelectMany(BuildRentalLines).ToList();
            List<InvoiceLine> lessonLines = booking.SkiLessons.Select(BuildSkiLessonLine).ToList();
            List<InvoiceLine> meetingRoomLines = booking.MeetingRooms.Select(BuildMeetingRoomLine).ToList();

            decimal insurance = booking.HasCancellationInsurance ? BookingController.CancellationInsurancePrice : 0m;

            // Accommodation
            if (booking.Accommodations.Any())
            {
                decimal listPrice = booking.AccommodationListPrice;
                decimal discount = booking.DiscountAmount;

                // Bookings created before list price and discount were saved: the accommodation is what remains of the total
                if (listPrice <= 0)
                {
                    listPrice = booking.TotalAmount - insurance
                        - rentalLines.Sum(l => l.AmountInclVat)
                        - lessonLines.Sum(l => l.AmountInclVat);
                    discount = 0m;
                }

                string rooms = string.Join(", ", booking.Accommodations.Select(ba =>
                    $"{ba.Accommodation?.AccommodationNumber} ({ba.Accommodation?.AccommodationType?.CategoryCode})"));

                int nights = booking.Accommodations.Sum(ba => (ba.EndDate.Date - ba.StartDate.Date).Days);

                lines.Add(CreateLine(
                    "Boende",
                    string.Join(", ", booking.Accommodations.Select(ba => ba.Accommodation?.AccommodationNumber)),
                    $"Boende {rooms} {checkIn:yyyy-MM-dd} – {checkOut:yyyy-MM-dd}",
                    nights,
                    "nätter",
                    listPrice,
                    discount,
                    VatRates.Accommodation));

                if (discount > 0)
                {
                    discounts.Add(new AppliedDiscount
                    {
                        Description = booking.Customer is CorporateCustomer ? "Företagsrabatt" : "Privatkundsrabatt",
                        AppliesTo = "Boende",
                        Rate = booking.DiscountRate,
                        AmountInclVat = discount
                    });
                }
            }

            if (insurance > 0)
            {
                lines.Add(CreateLine("Avbeställningsskydd", string.Empty, "Avbeställningsskydd", 1, "st", insurance, 0m, VatRates.CancellationInsurance));
            }

            lines.AddRange(rentalLines);
            lines.AddRange(lessonLines);
            lines.AddRange(meetingRoomLines);

            decimal totalInclVat = lines.Sum(l => l.AmountInclVat);

            return new BookingInvoice
            {
                BookingID = booking.BookingID,
                BookingDate = booking.BookingDate,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Customer = BuildCustomer(booking.Customer),
                InvoiceType = booking.Customer is CorporateCustomer
                    ? "Företagsfaktura – moms specificeras per momssats"
                    : "Privatkund – priser inklusive moms",
                Lines = lines,
                Discounts = discounts,
                VatSummary = SummarizeVat(lines),
                TotalExclVat = lines.Sum(l => l.AmountExclVat),
                TotalVat = lines.Sum(l => l.VatAmount),
                TotalInclVat = totalInclVat,
                BookingTotalAmount = booking.TotalAmount,
                MatchesBookingTotal = totalInclVat == booking.TotalAmount
            };
        }

        /// <summary>
        /// One line per rented item. If the price list has changed since the rental, the saved rental total is used instead.
        /// </summary>
        private static IEnumerable<InvoiceLine> BuildRentalLines(Rental rental)
        {
            int days = SkiShopController.CountRentalDays(rental.StartDate, rental.EndDate);
            string period = $"{rental.StartDate:yyyy-MM-dd} – {rental.EndDate:yyyy-MM-dd}";

            var itemPrices = rental.EquipmentItems
                .Select(item => (item, price: SkiShopController.CalculateRentalPrice(
                    item.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days)))
                .ToList();

            bool pricesMatch = itemPrices.Count > 0
                && itemPrices.All(p => p.price.HasValue)
                && itemPrices.Sum(p => p.price!.Value) == rental.TotalPrice;

            if (pricesMatch)
            {
                return itemPrices.Select(p => CreateLine(
                    "Utrustning",
                    p.item.ArticleNumber,
                    $"{p.item.Equipment?.DisplayName} {p.item.Size} {period}".Trim(),
                    days,
                    "dagar",
                    p.price!.Value,
                    0m,
                    VatRates.EquipmentRental));
            }

            string articles = string.Join(", ", rental.EquipmentItems.Select(i => i.ArticleNumber));

            return new[]
            {
                CreateLine("Utrustning", articles, $"Uthyrning {rental.RentalID} {period}", days, "dagar",
                    rental.TotalPrice, 0m, VatRates.EquipmentRental)
            };
        }

        private static InvoiceLine BuildSkiLessonLine(SkiLessonBooking lessonBooking)
        {
            SkiLessonSession? session = lessonBooking.SkiLessonSession;
            SkiLesson? lesson = session?.SkiLesson;

            return CreateLine(
                "Skidskola",
                lesson?.SkiLessonID.ToString() ?? string.Empty,
                $"Skidskola {lesson?.Level} ({lesson?.Days}) vecka {session?.WeekNumber} {session?.TimeSlot}".Trim(),
                1,
                "st",
                lesson?.Price ?? 0m,
                0m,
                VatRates.SkiLesson);
        }

        /// <summary>
        /// Meeting rooms are priced per day using the room's "dygn" season price for each day's week.
        /// </summary>
        private static InvoiceLine BuildMeetingRoomLine(BookingMeetingRoom meetingRoomBooking)
        {
            MeetingRoom? room = meetingRoomBooking.meetingRoom;
            DateTime start = meetingRoomBooking.StartDate.Date;
            DateTime end = meetingRoomBooking.EndDate.Date < start ? start : meetingRoomBooking.EndDate.Date;

            int days = (end - start).Days + 1;
            decimal price = 0m;

            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                int week = ISOWeek.GetWeekOfYear(day);
                price += room?.SeasonPrices
                    .FirstOrDefault(p => p.WeekNumber == week && p.PriceType == "dygn")?.Price ?? 0m;
            }

            return CreateLine(
                "Konferensrum",
                room?.MeetingRoomNumber ?? string.Empty,
                $"Konferensrum {room?.MeetingRoomNumber} {start:yyyy-MM-dd} – {end:yyyy-MM-dd}",
                days,
                "dygn",
                price,
                0m,
                VatRates.MeetingRoom);
        }

        private static InvoiceLine CreateLine(string articleType, string articleNumber, string description,
            int quantity, string unit, decimal listPriceInclVat, decimal discountInclVat, decimal vatRate)
        {
            decimal amountInclVat = listPriceInclVat - discountInclVat;
            (decimal amountExclVat, decimal vatAmount) = VatRates.SplitVat(amountInclVat, vatRate);

            return new InvoiceLine
            {
                ArticleType = articleType,
                ArticleNumber = articleNumber,
                Description = description,
                Quantity = quantity,
                Unit = unit,
                ListPriceInclVat = listPriceInclVat,
                DiscountInclVat = discountInclVat,
                AmountInclVat = amountInclVat,
                AmountExclVat = amountExclVat,
                VatRate = vatRate,
                VatAmount = vatAmount
            };
        }

        private static List<VatSummary> SummarizeVat(IEnumerable<InvoiceLine> lines)
        {
            return lines
                .GroupBy(l => l.VatRate)
                .OrderByDescending(g => g.Key)
                .Select(g => new VatSummary
                {
                    VatRate = g.Key,
                    AmountExclVat = g.Sum(l => l.AmountExclVat),
                    VatAmount = g.Sum(l => l.VatAmount),
                    AmountInclVat = g.Sum(l => l.AmountInclVat)
                })
                .ToList();
        }

        private static InvoiceCustomer BuildCustomer(Customer? customer)
        {
            if (customer == null)
            {
                return new InvoiceCustomer { CustomerType = "Okänd" };
            }

            var invoiceCustomer = new InvoiceCustomer
            {
                CustomerID = customer.CustomerID,
                Name = customer.DisplayName,
                Email = customer.Email,
                Address = customer.Address
            };

            if (customer is CorporateCustomer corporate)
            {
                invoiceCustomer.CustomerType = "Företagskund";
                invoiceCustomer.OrganisationNumber = corporate.OrganisationNumber;
                invoiceCustomer.ContactPerson = corporate.ContactPerson;
            }
            else
            {
                invoiceCustomer.CustomerType = "Privatkund";
            }

            return invoiceCustomer;
        }
    }
}
