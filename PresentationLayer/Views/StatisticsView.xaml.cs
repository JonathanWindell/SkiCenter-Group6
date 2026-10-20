using BusinessLayer.Controllers;
using BusinessLayer.Models;
using EntityLayer;
using Microsoft.Win32;
using PresentationLayer.Controls;
using PresentationLayer.Models;
using ServiceLayer.Pdf;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Statistics dashboard for the marketing manager: occupancy and sales per month,
    /// filtered by period and accommodation type and compared with the previous year.
    /// </summary>
    public partial class StatisticsView : Window
    {
        private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

        private readonly StatisticsController _statisticsController;
        private readonly ListReportController _listReportController;
        private readonly PdfReportService _pdfReportService;

        // The statistics currently shown, used for the statistics PDF
        private StatisticsReport? _currentReport;

        public StatisticsView(StatisticsController statisticsController, ListReportController listReportController, PdfReportService pdfReportService)
        {
            InitializeComponent();
            _statisticsController = statisticsController;
            _listReportController = listReportController;
            _pdfReportService = pdfReportService;

            ShowStatisticsButton.Click += (_, _) => LoadStatistics();
            CompareCheckBox.Click += (_, _) => LoadStatistics();

            StatisticsPdfButton.Click += StatisticsPdfButton_Click;
            CustomerRegisterPdfButton.Click += (_, _) => SaveReport(
                "Kundregister", () => _listReportController.CreateCustomerRegister(), _pdfReportService.CreateCustomerRegisterPdf);
            EquipmentRegisterPdfButton.Click += (_, _) => SaveReport(
                "Skidutrustningsregister", () => _listReportController.CreateEquipmentRegister(), _pdfReportService.CreateEquipmentRegisterPdf);
            PriceListPdfButton.Click += (_, _) => SaveReport(
                "Prislistor", () => _listReportController.CreatePriceList(), _pdfReportService.CreatePriceListPdf);

            Loaded += StatisticsView_Loaded;
        }

        /// <summary>
        /// Checks access, fills the filters and shows the default period.
        /// </summary>
        private void StatisticsView_Loaded(object sender, RoutedEventArgs e)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanViewStatistics(user.Role))
            {
                MessageBox.Show("Endast marknadschef och systemadministratör har tillgång till statistik.");
                Close();
                return;
            }

            FillMonthFilters();
            FillAccommodationTypeFilter();
            LoadStatistics();
        }

        /// <summary>
        /// Months from three years back to one year ahead. Default: two months back to three months ahead.
        /// </summary>
        private void FillMonthFilters()
        {
            DateTime thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            for (DateTime month = thisMonth.AddYears(-3); month <= thisMonth.AddYears(1); month = month.AddMonths(1))
            {
                FromMonthComboBox.Items.Add(new ComboBoxItem { Content = MonthLabel(month), Tag = month });
                ToMonthComboBox.Items.Add(new ComboBoxItem { Content = MonthLabel(month), Tag = month });
            }

            SelectMonth(FromMonthComboBox, thisMonth.AddMonths(-2));
            SelectMonth(ToMonthComboBox, thisMonth.AddMonths(3));
        }

        private static void SelectMonth(ComboBox comboBox, DateTime month)
        {
            comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag is DateTime m && m == month);
        }

        private void FillAccommodationTypeFilter()
        {
            AccommodationTypeComboBox.Items.Add(new ComboBoxItem { Content = "Alla boendetyper", Tag = null });

            try
            {
                foreach (AccommodationType type in _statisticsController.GetAccommodationTypes())
                {
                    AccommodationTypeComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{type.CategoryCode} – {type.Description}",
                        Tag = type.AccommodationTypeID
                    });
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Kunde inte hämta boendetyper: {ex.Message}";
            }

            AccommodationTypeComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Loads the statistics for the selected filters and updates tiles, charts and table.
        /// </summary>
        private void LoadStatistics()
        {
            MessageTextBlock.Text = string.Empty;

            if ((FromMonthComboBox.SelectedItem as ComboBoxItem)?.Tag is not DateTime fromMonth
                || (ToMonthComboBox.SelectedItem as ComboBoxItem)?.Tag is not DateTime toMonth)
            {
                MessageTextBlock.Text = "Välj start- och slutmånad.";
                return;
            }

            int? accommodationTypeId = (AccommodationTypeComboBox.SelectedItem as ComboBoxItem)?.Tag as int?;
            bool compare = CompareCheckBox.IsChecked == true;

            (bool success, string message, StatisticsReport? report) result;

            try
            {
                result = _statisticsController.GetStatistics(fromMonth, toMonth, accommodationTypeId);
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Kunde inte hämta statistik: {ex.Message}";
                return;
            }

            if (!result.success || result.report == null)
            {
                _currentReport = null;
                MessageTextBlock.Text = result.message;
                return;
            }

            StatisticsReport report = result.report;
            _currentReport = report;

            ShowTiles(report, compare);
            ShowCharts(report, compare);
            ShowTable(report, compare);
        }

        private void ShowTiles(StatisticsReport report, bool compare)
        {
            PeriodSummary current = report.CurrentTotals;
            PeriodSummary previous = report.PreviousTotals;

            OccupancyValueTextBlock.Text = Percent(current.OccupancyRate);
            SalesValueTextBlock.Text = $"{current.TotalSales.ToString("N0", Swedish)} kr";
            BookingsValueTextBlock.Text = current.BookingCount.ToString(Swedish);
            NightsValueTextBlock.Text = current.BookedNights.ToString("N0", Swedish);

            OccupancyCompareTextBlock.Text = compare
                ? $"Fg. år: {Percent(previous.OccupancyRate)} ({PointsChange(report.OccupancyChange)})"
                : string.Empty;
            SalesCompareTextBlock.Text = compare
                ? $"Fg. år: {previous.TotalSales.ToString("N0", Swedish)} kr ({Change(report.SalesChangePercent)})"
                : string.Empty;
            BookingsCompareTextBlock.Text = compare
                ? $"Fg. år: {previous.BookingCount} ({Change(report.BookingCountChangePercent)})"
                : string.Empty;
            NightsCompareTextBlock.Text = compare
                ? $"Fg. år: {previous.BookedNights.ToString("N0", Swedish)} ({Change(report.BookedNightsChangePercent)})"
                : string.Empty;
        }

        private void ShowCharts(StatisticsReport report, bool compare)
        {
            List<BarChartPoint> occupancy = report.CurrentPeriod
                .Select((month, i) => new BarChartPoint(
                    ShortMonthLabel(month.Month),
                    (double)month.OccupancyRate,
                    (double)report.PreviousPeriod[i].OccupancyRate))
                .ToList();

            List<BarChartPoint> sales = report.CurrentPeriod
                .Select((month, i) => new BarChartPoint(
                    ShortMonthLabel(month.Month),
                    (double)month.TotalSales,
                    (double)report.PreviousPeriod[i].TotalSales))
                .ToList();

            // Scales to the highest value so low occupancy is still visible; the labels show the exact percentage
            OccupancyChart.SetData(occupancy, value => value.ToString("P1", Swedish), null, compare);
            SalesChart.SetData(sales, value => ShortAmount(value), null, compare);
        }

        private void ShowTable(StatisticsReport report, bool compare)
        {
            PreviousOccupancyColumn.Visibility = compare ? Visibility.Visible : Visibility.Collapsed;
            PreviousSalesColumn.Visibility = compare ? Visibility.Visible : Visibility.Collapsed;
            SalesChangeColumn.Visibility = compare ? Visibility.Visible : Visibility.Collapsed;

            StatisticsDataGrid.ItemsSource = report.CurrentPeriod
                .Select((month, i) =>
                {
                    MonthlyStatistic previous = report.PreviousPeriod[i];

                    return new StatisticsRow
                    {
                        Month = MonthLabel(month.Month),
                        Occupancy = Percent(month.OccupancyRate),
                        PreviousOccupancy = Percent(previous.OccupancyRate),
                        BookedNights = month.BookedNights,
                        BookingCount = month.BookingCount,
                        AccommodationSales = month.AccommodationSales.ToString("N0", Swedish),
                        EquipmentSales = month.EquipmentSales.ToString("N0", Swedish),
                        SkiSchoolSales = month.SkiSchoolSales.ToString("N0", Swedish),
                        TotalSales = month.TotalSales.ToString("N0", Swedish),
                        PreviousSales = previous.TotalSales.ToString("N0", Swedish),
                        SalesChange = Change(StatisticsReport.PercentChange(month.TotalSales, previous.TotalSales))
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Saves the statistics currently shown, with the same filters, as a PDF.
        /// </summary>
        private void StatisticsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReport == null)
            {
                MessageTextBlock.Text = "Visa statistik innan rapporten skapas.";
                return;
            }

            StatisticsReport statistics = _currentReport;
            bool compare = CompareCheckBox.IsChecked == true;

            SaveReport("Statistikrapport", () => _listReportController.CreateStatisticsReport(statistics, compare), _pdfReportService.CreateStatisticsPdf);
        }

        /// <summary>
        /// Creates a report, asks where to save the PDF, saves it and opens it in the PDF viewer.
        /// </summary>
        private void SaveReport<TReport>(string reportName, Func<(bool success, string message, TReport? report)> createReport, Func<TReport, byte[]> createPdf)
            where TReport : class
        {
            MessageTextBlock.Text = string.Empty;

            (bool success, string message, TReport? report) result;

            try
            {
                result = createReport();
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Kunde inte hämta data till rapporten: {ex.Message}";
                return;
            }

            if (!result.success || result.report == null)
            {
                MessageTextBlock.Text = result.message;
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = $"Spara {reportName.ToLower(Swedish)}",
                Filter = "PDF-fil (*.pdf)|*.pdf",
                FileName = $"{reportName}_{DateTime.Today:yyyy-MM-dd}.pdf"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllBytes(dialog.FileName, createPdf(result.report));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapporten kunde inte sparas: {ex.Message}");
                return;
            }

            try
            {
                // Open the saved report in the default PDF viewer
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception)
            {
                MessageBox.Show($"Rapporten är sparad men kunde inte öppnas:\n{dialog.FileName}");
            }
        }

        private static string MonthLabel(DateTime month) => Swedish.TextInfo.ToTitleCase(month.ToString("MMMM yyyy", Swedish));

        private static string ShortMonthLabel(DateTime month) => month.ToString("MMM yy", Swedish);

        private static string Percent(decimal rate) => rate.ToString("P1", Swedish);

        private static string PointsChange(decimal change)
        {
            decimal points = Math.Round(change * 100, 1);
            return $"{(points >= 0 ? "+" : "")}{points.ToString("0.0", Swedish)} p.e.";
        }

        private static string Change(decimal? change)
        {
            if (!change.HasValue)
            {
                return "ingen jämförelse";
            }

            return $"{(change.Value >= 0 ? "+" : "")}{change.Value.ToString("P0", Swedish)}";
        }

        private static string ShortAmount(double value)
        {
            if (value >= 1_000_000) return $"{(value / 1_000_000).ToString("0.#", Swedish)} mkr";
            if (value >= 1_000) return $"{(value / 1_000).ToString("0.#", Swedish)} tkr";
            return value.ToString("0", Swedish);
        }

        // Close this window and return to the dashboard.
        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
