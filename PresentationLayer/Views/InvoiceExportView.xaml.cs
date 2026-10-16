using BusinessLayer.Controllers;
using BusinessLayer.Models;
using EntityLayer;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Lets finance staff (marketing manager) generate invoicing data for completed bookings and export it as JSON.
    /// </summary>
    public partial class InvoiceExportView : Window
    {
        private readonly ReportController _reportController;

        private InvoiceExport? _currentExport;
        private string _currentJson = string.Empty;

        public InvoiceExportView(ReportController reportController)
        {
            InitializeComponent();
            _reportController = reportController;

            // Default period: the current month up to today
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;

            GenerateButton.Click += GenerateButton_Click;
            ExportJsonButton.Click += ExportJsonButton_Click;

            Loaded += InvoiceExportView_Loaded;
        }

        /// <summary>
        /// Checks access before showing any data. Only marketing managers and system administrators may use this view.
        /// </summary>
        private void InvoiceExportView_Loaded(object sender, RoutedEventArgs e)
        {
            Staff? user = LoginController.CurrentLoggedInUser;

            if (user == null || !StaffController.CanExportInvoices(user.Role))
            {
                MessageBox.Show("Endast marknadschef och systemadministratör har tillgång till faktureringsunderlag.");
                Close();
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            ClearExport();

            if (FromDatePicker.SelectedDate is not DateTime fromDate || ToDatePicker.SelectedDate is not DateTime toDate)
            {
                MessageTextBlock.Text = "Välj både start- och slutdatum.";
                return;
            }

            InvoiceCustomerFilter filter = (CustomerTypeComboBox.SelectedItem as ComboBoxItem)?.Tag switch
            {
                "Private" => InvoiceCustomerFilter.Private,
                "Corporate" => InvoiceCustomerFilter.Corporate,
                _ => InvoiceCustomerFilter.All
            };

            (bool success, string message, InvoiceExport? export) result;

            try
            {
                result = _reportController.GenerateInvoiceExport(fromDate, toDate, filter);
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Kunde inte hämta faktureringsunderlag: {ex.Message}";
                return;
            }

            MessageTextBlock.Text = result.message;

            if (!result.success || result.export == null)
            {
                return;
            }

            _currentExport = result.export;
            _currentJson = ReportController.ToJson(result.export);

            BookingsDataGrid.ItemsSource = result.export.Bookings;
            JsonPreviewTextBox.Text = _currentJson;
            TotalsTextBlock.Text =
                $"{result.export.BookingCount} bokning(ar) – exkl. moms {result.export.TotalExclVat:N2} kr, " +
                $"moms {result.export.TotalVat:N2} kr, inkl. moms {result.export.TotalInclVat:N2} kr";

            int mismatches = result.export.Bookings.Count(b => !b.MatchesBookingTotal);
            if (mismatches > 0)
            {
                MessageTextBlock.Text += $" Obs: {mismatches} bokning(ar) stämmer inte med bokningens totalbelopp.";
            }

            ExportJsonButton.IsEnabled = result.export.BookingCount > 0;
        }

        /// <summary>
        /// Saves the current export as a JSON file chosen by the user.
        /// </summary>
        private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentExport == null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Spara faktureringsunderlag",
                Filter = "JSON-fil (*.json)|*.json",
                FileName = $"faktureringsunderlag_{_currentExport.PeriodFrom:yyyy-MM-dd}_{_currentExport.PeriodTo:yyyy-MM-dd}.json"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, _currentJson, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filen kunde inte sparas: {ex.Message}");
                return;
            }

            MessageBox.Show(
                $"Faktureringsunderlaget för {_currentExport.BookingCount} bokning(ar) har sparats:\n{dialog.FileName}",
                "Export klar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearExport()
        {
            _currentExport = null;
            _currentJson = string.Empty;
            BookingsDataGrid.ItemsSource = null;
            JsonPreviewTextBox.Text = string.Empty;
            TotalsTextBlock.Text = string.Empty;
            ExportJsonButton.IsEnabled = false;
        }
    }
}
