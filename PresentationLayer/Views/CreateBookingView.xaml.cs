using BusinessLayer;
using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;


namespace PresentationLayer.Views
{
    public partial class CreateBookingView : Window
    {
        private readonly CustomerController _customerController;
        private readonly BookingController _bookingController;
        private readonly IServiceProvider _serviceProvider;

        private Customer? _selectedCustomer;
        private decimal _accommodationPrice = 0m;
        private bool _hasCalculatedPrice = false;

        private DateTime? _searchedStartDate;
        private DateTime? _searchedEndDate;
        private int _searchedNumberOfGuests;

        public CreateBookingView(
            CustomerController customerController,
            BookingController bookingController,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _customerController = customerController;
            _bookingController = bookingController;
            _serviceProvider = serviceProvider;

            StartDatePicker.DisplayDateStart = DateTime.Today;
            EndDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);

            // Invalidate the search when booking details change.
            StartDatePicker.SelectedDateChanged += BookingDetails_Changed;
            EndDatePicker.SelectedDateChanged += BookingDetails_Changed;
            NumberOfGuestsTextBox.TextChanged += BookingDetails_Changed;
        }

        /// <summary>
        /// Searches for an existing customer.
        /// </summary>
        private void SearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            string searchItem = CustomerSearchTextBox.Text.Trim();

            _selectedCustomer = null;
            CustomerResultsListBox.ItemsSource = null;
            SelectedCustomerTextBlock.Text = "Ingen kund vald.";

            CreateBookingButton.IsEnabled = false;

            try
            {
                UpdateBookingPrice();

                if (string.IsNullOrWhiteSpace(searchItem))
                {
                    MessageBox.Show(
                        "Ange något att söka efter.",
                        "Sök kund",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                Customer? customer =
                    _customerController.SearchCustomer(searchItem);

                if (customer == null)
                {
                    MessageBox.Show(
                        "Ingen kund hittades.",
                        "Sök kund",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                _selectedCustomer = customer;

                string customerName = customer is PrivateCustomer pc
                    ? $"{pc.FirstName} {pc.LastName}"
                    : customer is CorporateCustomer cc
                        ? cc.CompanyName
                        : $"Kund #{customer.CustomerID}";

                SelectedCustomerTextBlock.Text =
                    $"Vald kund: {customerName} ({customer.Email})";

                CustomerResultsListBox.ItemsSource =
                    new[] { new { DisplayName = customerName } };

                UpdateBookingPrice();
            }
            catch (Exception ex)
            {
                CreateBookingButton.IsEnabled = false;

                MessageBox.Show(
                    $"Kunde inte söka efter kunden eller beräkna rabatten.\n{ex.Message}",
                    "Fel vid kundsökning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the customer registration window.
        /// </summary>
        private void RegisterCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomerRegistrationView registrationView =
                    _serviceProvider.GetRequiredService<CustomerRegistrationView>();

                registrationView.Owner = this;
                registrationView.ShowDialog();

                _selectedCustomer = null;
                CustomerResultsListBox.ItemsSource = null;

                SelectedCustomerTextBlock.Text =
                    "Sök efter den registrerade kunden för att fortsätta.";

                UpdateBookingPrice();
            }
            catch (Exception ex)
            {
                CreateBookingButton.IsEnabled = false;

                MessageBox.Show(
                    ex.Message,
                    "Kundregistrering",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clears the accommodation selection when booking details change.
        /// </summary>
        private void BookingDetails_Changed(object sender, EventArgs e)
        {
            InvalidateAccommodationSearch();
        }

        /// <summary>
        /// Clears previously calculated accommodation data.
        /// </summary>
        private void InvalidateAccommodationSearch()
        {
            _searchedStartDate = null;
            _searchedEndDate = null;
            _searchedNumberOfGuests = 0;

            _accommodationPrice = 0m;
            _hasCalculatedPrice = false;

            AvailableAccommodationsDataGrid.ItemsSource = null;
            CreateBookingButton.IsEnabled = false;

            BookingPriceTextBlock.Text =
                "Sök efter lediga boenden och välj ett boende.";
        }

        /// <summary>
        /// Searches for available accommodations.
        /// </summary>
        private void SearchAccommodations_Click(object sender, RoutedEventArgs e)
        {
            InvalidateAccommodationSearch();

            if (!StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Välj både incheckningsdatum och utcheckningsdatum.",
                    "Saknade datum",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
            DateTime endDate = EndDatePicker.SelectedDate.Value.Date;

            if (startDate < DateTime.Today)
            {
                MessageBox.Show(
                    "Incheckningsdatum kan inte vara tidigare än idag.",
                    "Ogiltigt datum",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (endDate <= startDate)
            {
                MessageBox.Show(
                    "Utcheckningsdatum måste vara efter incheckningsdatum.",
                    "Ogiltigt datum",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(
                    NumberOfGuestsTextBox.Text.Trim(),
                    out int numberOfGuests) ||
                numberOfGuests <= 0)
            {
                MessageBox.Show(
                    "Ange ett giltigt antal gäster (minst 1).",
                    "Ogiltigt antal gäster",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var availableAccommodations =
                    _bookingController.GetAvailableAccommodations(
                        startDate,
                        endDate,
                        numberOfGuests).ToList();

                _searchedStartDate = startDate;
                _searchedEndDate = endDate;
                _searchedNumberOfGuests = numberOfGuests;

                AvailableAccommodationsDataGrid.ItemsSource =
                    availableAccommodations;

                if (!availableAccommodations.Any())
                {
                    MessageBox.Show(
                        "Inga lediga boenden hittades för de valda datumen och antalet gäster.",
                        "Inga lediga boenden",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte söka efter lediga boenden.\n{ex.Message}",
                    "Fel vid sökning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Calculates the price when an accommodation is selected.
        /// </summary>
        private void AvailableAccommodations_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            CreateBookingButton.IsEnabled = false;

            _accommodationPrice = 0m;
            _hasCalculatedPrice = false;

            if (AvailableAccommodationsDataGrid.SelectedItem
                is not Accommodation selectedAccommodation)
            {
                BookingPriceTextBlock.Text =
                    "Pris beräknas när boende har valts.";
                return;
            }

            if (!_searchedStartDate.HasValue ||
                !_searchedEndDate.HasValue)
            {
                BookingPriceTextBlock.Text =
                    "Sök efter lediga boenden igen.";
                return;
            }

            try
            {
                _accommodationPrice =
                    _bookingController.CalculateAccommodationPrice(
                        selectedAccommodation.AccommodationTypeID,
                        _searchedStartDate.Value,
                        _searchedEndDate.Value);

                _hasCalculatedPrice = true;

                UpdateBookingPrice();
            }
            catch (Exception ex)
            {
                _accommodationPrice = 0m;
                _hasCalculatedPrice = false;
                CreateBookingButton.IsEnabled = false;

                BookingPriceTextBlock.Text =
                    "Kunde inte beräkna priset.";

                MessageBox.Show(
                    ex.Message,
                    "Prisberäkning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Updates the price when cancellation insurance changes.
        /// </summary>
        private void CancellationInsurance_Changed(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                UpdateBookingPrice();
            }
            catch (Exception ex)
            {
                CreateBookingButton.IsEnabled = false;

                MessageBox.Show(
                    ex.Message,
                    "Fel vid prisberäkning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Calculates the total price including discounts and insurance.
        /// </summary>
        private void UpdateBookingPrice()
        {
            CreateBookingButton.IsEnabled = false;

            if (!_hasCalculatedPrice)
            {
                BookingPriceTextBlock.Text =
                    "Pris beräknas när boende har valts.";
                return;
            }

            decimal discountAmount = 0m;

            // Calculate the discount based on customer type.
            if (_selectedCustomer is PrivateCustomer privateCustomer)
            {
                discountAmount =
                    _bookingController.CalculatePrivateCustomerDiscount(
                        privateCustomer,
                        _accommodationPrice,
                        DateTime.Today);
            }
            else if (_selectedCustomer is CorporateCustomer corporateCustomer)
            {
                discountAmount =
                    _bookingController.CalculateCorporateDiscount(
                        corporateCustomer,
                        _accommodationPrice);
            }

            decimal insurancePrice =
                CancellationInsuranceCheckBox.IsChecked == true
                    ? 300m
                    : 0m;

            decimal totalPrice =
                _accommodationPrice - discountAmount + insurancePrice;

            // Show a discount only when the amount is greater than zero.
            string discountText = discountAmount > 0
                ? $"-{discountAmount:N2} kr"
                : "0,00 kr";

            BookingPriceTextBlock.Text =
                $"Boende: {_accommodationPrice:N2} kr\n" +
                $"Rabatt: {discountText}\n" +
                $"Avbeställningsskydd: {insurancePrice:N2} kr\n" +
                $"Totalt: {totalPrice:N2} kr";

            // Enable booking creation only when all required data exists.
            CreateBookingButton.IsEnabled =
                _selectedCustomer != null &&
                _selectedCustomer.CustomerID > 0 &&
                AvailableAccommodationsDataGrid.SelectedItem is Accommodation &&
                _searchedStartDate.HasValue &&
                _searchedEndDate.HasValue &&
                _searchedNumberOfGuests > 0;
        }

        /// <summary>
        /// Creates and saves the selected accommodation booking.
        /// </summary>
        private void CreateBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show(
                    "Välj en kund innan du skapar bokningen.",
                    "Saknad kund",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (AvailableAccommodationsDataGrid.SelectedItem
                is not Accommodation selectedAccommodation)
            {
                MessageBox.Show(
                    "Välj ett boende innan du skapar bokningen.",
                    "Saknat boende",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!_hasCalculatedPrice ||
                !_searchedStartDate.HasValue ||
                !_searchedEndDate.HasValue ||
                _searchedNumberOfGuests <= 0)
            {
                MessageBox.Show(
                    "Sök efter lediga boenden och välj ett boende igen.",
                    "Ofullständig bokning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool hasCancellationInsurance =
                CancellationInsuranceCheckBox.IsChecked == true;

            MessageBoxResult confirmation = MessageBox.Show(
                "Vill du skapa bokningen för den valda kunden och boendet?",
                "Bekräfta bokning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                CreateBookingButton.IsEnabled = false;

                // The controller validates availability and recalculates
                // the price before saving the booking.
                Booking booking = _bookingController.CreateBooking(
                    _selectedCustomer,
                    selectedAccommodation,
                    _searchedStartDate.Value,
                    _searchedEndDate.Value,
                    _searchedNumberOfGuests,
                    hasCancellationInsurance);

                MessageBox.Show(
                    $"Bokningen har skapats!\n\n" +
                    $"Bokningsnummer: {booking.BookingID}\n" +
                    $"Status: Preliminär\n" +
                    $"Totalpris: {booking.TotalAmount:N2} kr",
                    "Bokning skapad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte skapa bokningen.\n{ex.Message}",
                    "Fel vid bokning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Require a new availability search after a failed save.
                InvalidateAccommodationSearch();
            }
        }

        /// <summary>
        /// Closes the booking window without saving.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
