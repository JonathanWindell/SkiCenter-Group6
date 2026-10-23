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

            var corporateCustomers = _customerController.GetAllCorporateCustomers();

            foreach (var c in corporateCustomers)
            {
                decimal displayDiscount = c.DiscountRate <= 1m && c.DiscountRate > 0m
                    ? c.DiscountRate * 100m
                    : c.DiscountRate;

                _customers.Add(new CorporateCustomerRow
                {
                    CustomerId = c.CustomerID,
                    CompanyName = c.CompanyName,
                    OrgNumber = c.OrganisationNumber,
                    ContactPerson = c.ContactPerson,
                    Phone = c.PhoneNumber,
                    Email = c.Email,
                    IsApproved = c.IsApproved,
                    OriginalIsApproved = c.IsApproved,
                    CreditLimit = c.CreditLimit,
                    OriginalCreditLimit = c.CreditLimit,
                    DiscountRate = displayDiscount,
                    OriginalDiscountRate = displayDiscount
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

            int pendingCount = view.Cast<CorporateCustomerRow>().Count(c => !c.IsApproved);

            CorpCustomerCountTextBlock.Text = pendingCount switch
            {
                0 => "Inga kunder väntar på godkännande.",
                1 => "1 kund väntar på godkännande",
                _ => $"{pendingCount} kunder väntar på godkännande"
            };
        }

        // Saves all changes made in the page in one go
        private void SaveRowsButton_Click(object sender, RoutedEventArgs e)
        {
            var changedRows = _customers.Where(c => c.IsChanged).ToList();

            if (!changedRows.Any())
            {
                MessageBox.Show("Inga ändringar har gjorts.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validation for each row
            foreach (var row in changedRows)
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

            int savedCount = 0;

            foreach (var row in changedRows)
            {
                bool success = _customerController.UpdateCorporateCustomer(
                    row.CustomerId,
                    row.IsApproved,
                    row.CreditLimit,
                    row.DiscountRate);

                if (success)
                {
                    savedCount++;
                }
            }

            if (savedCount > 0)
            {
                MessageBox.Show($"{savedCount} företagskund(er) har uppdaterats.", "Sparat", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Kunde inte spara ändringarna till databasen.", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            LoadData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
