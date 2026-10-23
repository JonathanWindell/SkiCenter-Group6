using BusinessLayer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ServiceLayer.Pdf
{
    /// <summary>
    /// Creates printable PDF reports. Every report has the same layout: a header, a summary box with the
    /// report's key messages, charts and tables, and page numbers in the footer.
    /// </summary>
    public class PdfReportService
    {
        private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

        // Colors from the application's theme
        private const string PrimaryColor = "#1E4F8C";       // Fjällblå
        private const string PreviousColor = "#52B788";      // Aurora grön (statistics in blue and green)
        private const string TextColor = "#2A2F36";          // Midnattsgrå
        private const string SecondaryTextColor = "#5C6670";
        private const string SummaryBackground = "#DCEAF7";  // Isblå
        private const string BorderColor = "#B7CFE8";
        private const string AlternateRowColor = "#F8FAFC";  // Snövit
        private const string AccentColor = "#52B788";        // Aurora grön accent line

        // The graphic profile's fonts: Montserrat for headings, Open Sans for body text
        private const string HeadingFont = "Montserrat";
        private const string BodyFont = "Open Sans";

        static PdfReportService()
        {
            // QuestPDF is free under the Community license for education and small organisations
            QuestPDF.Settings.License = LicenseType.Community;

            // Register the embedded brand fonts so the PDFs look the same on every computer
            var assembly = typeof(PdfReportService).Assembly;
            foreach (string resource in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
            {
                using Stream? stream = assembly.GetManifestResourceStream(resource);
                if (stream != null)
                {
                    FontManager.RegisterFont(stream);
                }
            }
        }

        /// <summary>
        /// The statistics view's content: key figures, charts per month and a table per month.
        /// </summary>
        public byte[] CreateStatisticsPdf(StatisticsPdfReport report)
        {
            StatisticsReport stats = report.Statistics;
            bool compare = report.CompareWithPreviousYear;

            return CreateDocument(report, landscape: false, content =>
            {
                content.Item().Text($"{stats.AccommodationTypeName}, {MonthName(stats.FromMonth)} – {MonthName(stats.ToMonth)}")
                    .FontSize(11).FontColor(SecondaryTextColor);

                content.Item().PaddingTop(10).Row(row =>
                {
                    PeriodSummary current = stats.CurrentTotals;
                    PeriodSummary previous = stats.PreviousTotals;

                    KeyFigure(row.RelativeItem(), "Beläggning", current.OccupancyRate.ToString("P1", Swedish),
                        compare ? $"Fg. år {previous.OccupancyRate.ToString("P1", Swedish)}" : null);
                    KeyFigure(row.RelativeItem(), "Försäljning exkl. moms", $"{current.TotalSales.ToString("N0", Swedish)} kr",
                        compare ? $"Fg. år {previous.TotalSales.ToString("N0", Swedish)} kr" : null);
                    KeyFigure(row.RelativeItem(), "Bokningar", current.BookingCount.ToString(Swedish),
                        compare ? $"Fg. år {previous.BookingCount}" : null);
                    KeyFigure(row.RelativeItem(), "Bokade nätter", current.BookedNights.ToString("N0", Swedish),
                        compare ? $"Fg. år {previous.BookedNights.ToString("N0", Swedish)}" : null);
                });

                List<(string, decimal, decimal?)> occupancy = stats.CurrentPeriod
                    .Select((m, i) => (ShortMonth(m.Month), m.OccupancyRate, compare ? stats.PreviousPeriod[i].OccupancyRate : (decimal?)null))
                    .ToList();
                List<(string, decimal, decimal?)> sales = stats.CurrentPeriod
                    .Select((m, i) => (ShortMonth(m.Month), m.TotalSales, compare ? stats.PreviousPeriod[i].TotalSales : (decimal?)null))
                    .ToList();

                content.Item().PaddingTop(14).Element(c => BarChart(c, "Beläggning per månad", occupancy, v => v.ToString("P1", Swedish), compare));
                content.Item().PaddingTop(14).Element(c => BarChart(c, "Försäljning per månad (kr exkl. moms)", sales, v => v.ToString("N0", Swedish), compare));

                content.Item().PaddingTop(14).Text("Per månad").FontSize(12).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);
                content.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn();
                        if (compare) c.RelativeColumn();
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        if (compare) c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h, "Månad");
                        HeaderCell(h, "Beläggning");
                        if (compare) HeaderCell(h, "Fg. år");
                        HeaderCell(h, "Nätter");
                        HeaderCell(h, "Boende");
                        HeaderCell(h, "Utrustning");
                        HeaderCell(h, "Skidskola");
                        HeaderCell(h, "Totalt");
                        if (compare) HeaderCell(h, "Fg. år");
                    });

                    for (int i = 0; i < stats.CurrentPeriod.Count; i++)
                    {
                        MonthlyStatistic m = stats.CurrentPeriod[i];
                        MonthlyStatistic p = stats.PreviousPeriod[i];
                        bool alternate = i % 2 == 1;

                        BodyCell(table, MonthName(m.Month), alternate);
                        BodyCell(table, m.OccupancyRate.ToString("P1", Swedish), alternate);
                        if (compare) BodyCell(table, p.OccupancyRate.ToString("P1", Swedish), alternate);
                        BodyCell(table, m.BookedNights.ToString(Swedish), alternate);
                        BodyCell(table, m.AccommodationSales.ToString("N0", Swedish), alternate);
                        BodyCell(table, m.EquipmentSales.ToString("N0", Swedish), alternate);
                        BodyCell(table, m.SkiSchoolSales.ToString("N0", Swedish), alternate);
                        BodyCell(table, m.TotalSales.ToString("N0", Swedish), alternate, bold: true);
                        if (compare) BodyCell(table, p.TotalSales.ToString("N0", Swedish), alternate);
                    }
                });
            });
        }

        /// <summary>
        /// All customers with contact details, credit limit and, for corporate customers, discount and approval.
        /// </summary>
        public byte[] CreateCustomerRegisterPdf(CustomerRegisterReport report)
        {
            return CreateDocument(report, landscape: true, content =>
            {
                var customerTypes = new List<(string, decimal, decimal?)>
                {
                    ("Privatkunder", report.PrivateCustomerCount, null),
                    ("Företagskunder", report.CorporateCustomerCount, null),
                    ("Väntar på godkännande", report.PendingCorporateCount, null)
                };

                content.Item().Element(c => BarChart(c, "Kunder per typ", customerTypes, v => v.ToString("N0", Swedish), false));

                content.Item().PaddingTop(14).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(35);
                        c.RelativeColumn(0.9f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn();
                        c.RelativeColumn(1.6f);
                        c.RelativeColumn();
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(0.6f);
                        c.RelativeColumn(0.7f);
                        c.RelativeColumn(0.6f);
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h, "ID");
                        HeaderCell(h, "Typ");
                        HeaderCell(h, "Namn / kontakt");
                        HeaderCell(h, "Org.nr");
                        HeaderCell(h, "E-post");
                        HeaderCell(h, "Telefon");
                        HeaderCell(h, "Adress");
                        HeaderCell(h, "Kreditgräns");
                        HeaderCell(h, "Rabatt");
                        HeaderCell(h, "Godkänd");
                        HeaderCell(h, "Bokn.");
                    });

                    for (int i = 0; i < report.Customers.Count; i++)
                    {
                        CustomerRegisterRow c = report.Customers[i];
                        bool alternate = i % 2 == 1;

                        BodyCell(table, c.CustomerID.ToString(Swedish), alternate);
                        BodyCell(table, c.CustomerType, alternate);
                        BodyCell(table, string.IsNullOrEmpty(c.ContactPerson) ? c.Name : $"{c.Name}\n{c.ContactPerson}", alternate);
                        BodyCell(table, c.OrganisationNumber ?? "–", alternate);
                        BodyCell(table, c.Email, alternate);
                        BodyCell(table, c.PhoneNumber, alternate);
                        BodyCell(table, c.Address, alternate);
                        BodyCell(table, c.CreditLimit.ToString("N0", Swedish), alternate);
                        BodyCell(table, c.DiscountRate.HasValue ? c.DiscountRate.Value.ToString("P0", Swedish) : "–", alternate);
                        BodyCell(table, c.IsApproved.HasValue ? (c.IsApproved.Value ? "Ja" : "Nej") : "–", alternate);
                        BodyCell(table, c.BookingCount.ToString(Swedish), alternate);
                    }
                });

                content.Item().PaddingTop(8).Text("Innehåller personuppgifter. Hanteras enligt GDPR och ska inte spridas utanför verksamheten.")
                    .FontSize(8).Italic().FontColor(SecondaryTextColor);
            });
        }

        /// <summary>
        /// Status per category and every item in the ski shop.
        /// </summary>
        public byte[] CreateEquipmentRegisterPdf(EquipmentRegisterReport report)
        {
            return CreateDocument(report, landscape: false, content =>
            {
                content.Item().Row(row =>
                {
                    KeyFigure(row.RelativeItem(), "Artiklar totalt", report.TotalItems.ToString("N0", Swedish), null);
                    KeyFigure(row.RelativeItem(), "Tillgängliga", $"{report.AvailableItems.ToString("N0", Swedish)} ({report.AvailableShare.ToString("P1", Swedish)})", null);
                    KeyFigure(row.RelativeItem(), "Uthyrda", report.RentedItems.ToString("N0", Swedish), null);
                    KeyFigure(row.RelativeItem(), "Behöver åtgärdas", report.NeedsActionItems.ToString("N0", Swedish), null);
                });

                List<(string, decimal, decimal?)> perCategory = report.Categories
                    .Select(c => (c.Category, (decimal)c.Total, (decimal?)null))
                    .ToList();

                content.Item().PaddingTop(14).Element(c => BarChart(c, "Antal artiklar per kategori", perCategory, v => v.ToString("N0", Swedish), false));

                content.Item().PaddingTop(14).Text("Status per kategori").FontSize(12).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);
                content.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.5f);
                        for (int i = 0; i < 5; i++) c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h, "Kategori");
                        HeaderCell(h, "Tillgängliga");
                        HeaderCell(h, "Uthyrda");
                        HeaderCell(h, "Skadade");
                        HeaderCell(h, "Service");
                        HeaderCell(h, "Totalt");
                    });

                    for (int i = 0; i < report.Categories.Count; i++)
                    {
                        EquipmentCategorySummary c = report.Categories[i];
                        bool alternate = i % 2 == 1;

                        BodyCell(table, c.Category, alternate, bold: true);
                        BodyCell(table, c.Available.ToString(Swedish), alternate);
                        BodyCell(table, c.Rented.ToString(Swedish), alternate);
                        BodyCell(table, c.Damaged.ToString(Swedish), alternate);
                        BodyCell(table, c.Maintenance.ToString(Swedish), alternate);
                        BodyCell(table, c.Total.ToString(Swedish), alternate, bold: true);
                    }
                });

                content.Item().PageBreak();
                content.Item().Text("Alla artiklar").FontSize(12).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);
                content.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn();
                        c.RelativeColumn(0.7f);
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h, "Kategori");
                        HeaderCell(h, "Modell");
                        HeaderCell(h, "Artikelnr");
                        HeaderCell(h, "Storlek");
                        HeaderCell(h, "Status");
                        HeaderCell(h, "Skick");
                    });

                    for (int i = 0; i < report.Items.Count; i++)
                    {
                        EquipmentRegisterRow item = report.Items[i];
                        bool alternate = i % 2 == 1;

                        BodyCell(table, item.Category, alternate);
                        BodyCell(table, item.Model, alternate);
                        BodyCell(table, item.ArticleNumber, alternate);
                        BodyCell(table, item.Size, alternate);
                        BodyCell(table, item.Status, alternate, bold: item.Status is "Skadad" or "Service");
                        BodyCell(table, item.Condition, alternate);
                    }
                });
            });
        }

        /// <summary>
        /// Current season prices per accommodation type, the equipment rental price list and ski school prices.
        /// </summary>
        public byte[] CreatePriceListPdf(PriceListReport report)
        {
            return CreateDocument(report, landscape: false, content =>
            {
                foreach (AccommodationPriceList list in report.AccommodationPrices)
                {
                    content.Item().PaddingTop(10).Text($"Boende: {list.AccommodationType}").FontSize(13).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);

                    if (list.PriceTypes.Contains("vecka"))
                    {
                        List<(string, decimal, decimal?)> weekly = list.Weeks
                            .Where(w => w.Prices.ContainsKey("vecka"))
                            .Select(w => ($"v. {w.WeekNumber}", w.Prices["vecka"], (decimal?)null))
                            .ToList();

                        content.Item().PaddingTop(6).Element(c => BarChart(c, "Veckopris per vecka (kr)", weekly, v => v.ToString("N0", Swedish), false, barHeight: 5));
                    }

                    content.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(60);
                            foreach (string _ in list.PriceTypes) c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h, "Vecka");
                            foreach (string priceType in list.PriceTypes) HeaderCell(h, $"Pris {priceType} (kr)");
                        });

                        for (int i = 0; i < list.Weeks.Count; i++)
                        {
                            WeekPriceRow week = list.Weeks[i];
                            bool alternate = i % 2 == 1;
                            bool highSeason = list.HighSeasonWeeks.Contains(week.WeekNumber);

                            BodyCell(table, week.WeekNumber.ToString(Swedish), alternate, bold: highSeason);
                            foreach (string priceType in list.PriceTypes)
                            {
                                BodyCell(table, week.Prices.TryGetValue(priceType, out decimal price) ? price.ToString("N0", Swedish) : "–", alternate, bold: highSeason);
                            }
                        }
                    });

                    content.Item().PageBreak();
                }

                if (report.EquipmentPrices.Count > 0)
                {
                    List<int> dayColumns = report.EquipmentPrices.SelectMany(e => e.PricesByDays.Keys).Distinct().OrderBy(d => d).ToList();

                    content.Item().Text("Uthyrning av utrustning").FontSize(13).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);
                    content.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn(1.3f);
                            foreach (int _ in dayColumns) c.RelativeColumn(0.7f);
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h, "Kategori");
                            HeaderCell(h, "Utrustning");
                            foreach (int days in dayColumns) HeaderCell(h, days == 1 ? "1 dag" : $"{days} dagar");
                        });

                        for (int i = 0; i < report.EquipmentPrices.Count; i++)
                        {
                            EquipmentPriceRow row = report.EquipmentPrices[i];
                            bool alternate = i % 2 == 1;

                            BodyCell(table, row.Category, alternate);
                            BodyCell(table, row.Model, alternate);
                            foreach (int days in dayColumns)
                            {
                                BodyCell(table, row.PricesByDays.TryGetValue(days, out decimal price) ? price.ToString("N0", Swedish) : "–", alternate);
                            }
                        }
                    });
                }

                if (report.SkiLessonPrices.Count > 0)
                {
                    content.Item().PaddingTop(16).Text("Skidskola").FontSize(13).SemiBold().FontFamily(HeadingFont).FontColor(TextColor);
                    content.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h, "Nivå");
                            HeaderCell(h, "Dagar");
                            HeaderCell(h, "Max deltagare");
                            HeaderCell(h, "Pris (kr)");
                        });

                        for (int i = 0; i < report.SkiLessonPrices.Count; i++)
                        {
                            SkiLessonPriceRow lesson = report.SkiLessonPrices[i];
                            bool alternate = i % 2 == 1;

                            BodyCell(table, lesson.Level, alternate);
                            BodyCell(table, lesson.Days, alternate);
                            BodyCell(table, lesson.MaxParticipants.ToString(Swedish), alternate);
                            BodyCell(table, lesson.Price.ToString("N0", Swedish), alternate, bold: true);
                        }
                    });
                }

                content.Item().PaddingTop(8).Text("Alla priser är inklusive moms. Fetmarkerade veckor är högsäsong.")
                    .FontSize(8).Italic().FontColor(SecondaryTextColor);
            });
        }

        /// <summary>
        /// Shared page layout: header with title and creator, summary box, the report content and page numbers.
        /// </summary>
        private static byte[] CreateDocument(ReportBase report, bool landscape, Action<ColumnDescriptor> composeContent)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(style => style.FontFamily(BodyFont).FontSize(9).FontColor(TextColor));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(title =>
                            {
                                title.Item().Text("Ski-Center").FontFamily(HeadingFont).FontSize(11).Bold().FontColor(PrimaryColor);
                                title.Item().Text(report.Title).FontFamily(HeadingFont).FontSize(20).Bold().FontColor(TextColor);
                            });

                            row.ConstantItem(200).AlignRight().AlignBottom().Column(info =>
                            {
                                info.Item().AlignRight().Text($"Skapad {report.CreatedAt.ToString("yyyy-MM-dd HH:mm", Swedish)}").FontSize(8);
                                info.Item().AlignRight().Text($"Av {report.CreatedBy}").FontSize(8);
                            });
                        });

                        header.Item().PaddingTop(6).PaddingBottom(10).LineHorizontal(2).LineColor(AccentColor);
                    });

                    page.Content().Column(content =>
                    {
                        // The report's main message comes first
                        content.Item().Background(SummaryBackground).Padding(10).Column(summary =>
                        {
                            summary.Item().Text("Sammanfattning").FontFamily(HeadingFont).FontSize(11).Bold().FontColor(PrimaryColor);

                            foreach (string message in report.KeyMessages)
                            {
                                summary.Item().PaddingTop(3).Text($"•  {message}").FontSize(10);
                            }
                        });

                        content.Item().PaddingTop(12).Column(composeContent);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(SecondaryTextColor));
                        text.Span("Sida ");
                        text.CurrentPageNumber();
                        text.Span(" av ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        /// <summary>
        /// Horizontal bar chart: one row per label, with an optional lighter bar for the previous year.
        /// </summary>
        private static void BarChart(IContainer container, string title, List<(string label, decimal current, decimal? previous)> data,
            Func<decimal, string> format, bool showPrevious, float barHeight = 9)
        {
            container.Border(1).BorderColor(BorderColor).Padding(10).Column(chart =>
            {
                chart.Item().Row(row =>
                {
                    row.RelativeItem().Text(title).FontSize(11).SemiBold().FontFamily(HeadingFont);

                    if (showPrevious)
                    {
                        row.AutoItem().AlignMiddle().Width(8).Height(8).Background(PrimaryColor);
                        row.AutoItem().PaddingLeft(3).PaddingRight(8).Text("Vald period").FontSize(8);
                        row.AutoItem().AlignMiddle().Width(8).Height(8).Background(PreviousColor);
                        row.AutoItem().PaddingLeft(3).Text("Föregående år").FontSize(8);
                    }
                });

                decimal max = data.Count == 0 ? 0 : data.Max(d => Math.Max(d.current, showPrevious ? d.previous ?? 0 : 0));

                if (max <= 0)
                {
                    chart.Item().PaddingTop(6).Text("Ingen data att visa.").Italic().FontColor(SecondaryTextColor);
                    return;
                }

                foreach ((string label, decimal current, decimal? previous) in data)
                {
                    chart.Item().PaddingTop(3).Row(row =>
                    {
                        row.ConstantItem(90).AlignMiddle().Text(label).FontSize(8);

                        row.RelativeItem().AlignMiddle().Column(bars =>
                        {
                            bars.Item().Element(c => Bar(c, current / max, PrimaryColor, barHeight));

                            if (showPrevious)
                            {
                                bars.Item().PaddingTop(1).Element(c => Bar(c, (previous ?? 0) / max, PreviousColor, barHeight * 0.7f));
                            }
                        });

                        row.ConstantItem(80).AlignRight().AlignMiddle().Text(text =>
                        {
                            text.Span(format(current)).FontSize(8).SemiBold();
                            if (showPrevious)
                            {
                                text.Span($"\n{format(previous ?? 0)}").FontSize(7).FontColor(SecondaryTextColor);
                            }
                        });
                    });
                }
            });
        }

        private static void Bar(IContainer container, decimal share, string color, float height)
        {
            float filled = (float)Math.Clamp(share, 0, 1);

            container.Height(height).Row(row =>
            {
                if (filled > 0)
                {
                    row.RelativeItem(filled).Background(color);
                }

                if (filled < 1)
                {
                    row.RelativeItem(1 - filled);
                }
            });
        }

        private static void KeyFigure(IContainer container, string title, string value, string? comparison)
        {
            container.PaddingRight(8).Border(1).BorderColor(BorderColor).Padding(8).Column(figure =>
            {
                figure.Item().Text(title).FontSize(8).FontColor(SecondaryTextColor);
                figure.Item().Text(value).FontSize(14).Bold();

                if (comparison != null)
                {
                    figure.Item().Text(comparison).FontSize(8);
                }
            });
        }

        private static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell().Background(PrimaryColor).PaddingVertical(4).PaddingHorizontal(4)
                .Text(text).FontSize(8).SemiBold().FontColor(Colors.White);
        }

        private static void BodyCell(TableDescriptor table, string text, bool alternate, bool bold = false)
        {
            var cell = table.Cell()
                .Background(alternate ? AlternateRowColor : Colors.White)
                .BorderBottom(0.5f).BorderColor(BorderColor)
                .PaddingVertical(3).PaddingHorizontal(4)
                .Text(text).FontSize(8);

            if (bold)
            {
                cell.SemiBold();
            }
        }

        private static string MonthName(DateTime month) => Swedish.TextInfo.ToTitleCase(month.ToString("MMMM yyyy", Swedish));

        private static string ShortMonth(DateTime month) => month.ToString("MMM yyyy", Swedish);
    }
}
