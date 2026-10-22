using BusinessLayer;
using BusinessLayer.Controllers;
using PresentationLayer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PresentationLayer.Views
{
    public partial class MarketingMgmtView : Window
    {
        private readonly CustomerController _customerController;
        private readonly BookingController _bookingController;
        private readonly ObservableCollection<CorporateCustomerRow> _customers = new();

        public MarketingMgmtView(CustomerController customerController, BookingController bookingController)
        {
            InitializeComponent();

            _customerController = customerController;
            _bookingController = bookingController;

            CollectionViewSource.GetDefaultView(_customers).Filter = MatchesFilter;
            CustomersDataGrid.ItemsSource = _customers;

            SearchFilterTextBox.TextChanged += SearchFilterTextBox_TextChanged;
            ResetFiltersButton.Click += ResetFiltersButton_Click;

            LoadData();
        }

        private void LoadData()
        {
            _customers.Clear();

            var pendingCompanies = _customerController.GetPendingCorporateCustomers();

            foreach (var c in pendingCompanies)
            {
                _customers.Add(new CorporateCustomerRow
                {
                    CustomerId = c.CustomerID,
                    CompanyName = c.CompanyName,
                    OrgNumber = c.OrganisationNumber,
                    ContactPerson = c.ContactPerson,
                    Phone = c.PhoneNumber,
                    Email = c.Email,
                    IsApproved = c.IsApproved,
                    CreditLimit = c.CreditLimit,
                    OriginalCreditLimit = c.CreditLimit,
                    DiscountRate = c.DiscountRate,
                    OriginalDiscountRate = c.DiscountRate
                });
            }

            CollectionViewSource.GetDefaultView(_customers)?.Refresh();
            UpdateCountText();
        }

        private bool MatchesFilter(object item)
        {
            if (item is not CorporateCustomerRow row)
                return false;

            string search = SearchFilterTextBox.Text.Trim();
            if (string.IsNullOrEmpty(search))
                return true;

            return (!string.IsNullOrEmpty(row.CompanyName) && row.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(row.OrgNumber) && row.OrgNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(row.ContactPerson) && row.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private void SearchFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_customers)?.Refresh();
            UpdateCountText();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchFilterTextBox.Text = string.Empty;
            CollectionViewSource.GetDefaultView(_customers)?.Refresh();
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            var view = CollectionViewSource.GetDefaultView(_customers);
            int count = view.Cast<CorporateCustomerRow>().Count();

            CorpCustomerCountTextBlock.Text = count switch
            {
                0 => "Inga kunder väntar på godkännande.",
                1 => "1 kund väntar på godkännande",
                _ => $"{count} kunder väntar på godkännande"
            };
        }

        // Saves all changes made in the page in one go
        private void SaveRowsButton_Click(object sender, RoutedEventArgs e)
        {
            // To find rows that awaits approval
            var rowsToApprove = _customers.Where(c => c.IsApproved).ToList();

            if (!rowsToApprove.Any())
            {
                MessageBox.Show("Inga kunder har markerats för godkännande.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validation before saving
            foreach (var row in rowsToApprove)
            {
                if (row.CreditLimit < 0)
                {
                    MessageBox.Show($"Kreditgränsen för {row.CompanyName} kan inte vara negativ.", "Valideringsfel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (row.DiscountRate < 0 || row.DiscountRate > 100)
                {
                    MessageBox.Show($"Rabatten för {row.CompanyName} måste vara mellan 0 och 100 %.", "Valideringsfel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var confirmResult = MessageBox.Show(
                $"Är du säker på att du vill godkänna {rowsToApprove.Count} företagskund(er) och uppdatera deras avtal i databasen?",
                "Bekräfta godkännande",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            int successCount = 0;
            int confirmedBookingsTotal = 0;

            foreach (var row in rowsToApprove)
            {
                bool approved = _customerController.ApproveCorporateCustomer(row.CustomerId, row.CreditLimit, row.DiscountRate);

                if (approved)
                {
                    successCount++;

                    // Makes preliminray booking a confirmed booking
                    confirmedBookingsTotal += _bookingController.ConfirmPreliminaryBookingsForCustomer(row.CustomerId);
                }
            }

            // Feedback to user
            if (successCount > 0)
            {
                string bookingMsg = confirmedBookingsTotal > 0
                    ? $"\nDessutom gjordes {confirmedBookingsTotal} preliminära bokningar definitiva."
                    : "";

                MessageBox.Show(
                    $"{successCount} av {rowsToApprove.Count} företagskunder har godkänts och sparats i databasen!{bookingMsg}",
                    "Klart",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Inga ändringar kunde sparas till databasen.", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            LoadData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
