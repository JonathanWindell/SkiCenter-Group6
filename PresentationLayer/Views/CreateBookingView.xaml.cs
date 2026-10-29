
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
        #region Fields and constructor

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

            // Button should be "clickable" so error messages can be shown. 
            CreateBookingButton.IsEnabled = true;

            // Reset accommodation search if booking information is changed. 
            StartDatePicker.SelectedDateChanged += BookingDetails_Changed;
            EndDatePicker.SelectedDateChanged += BookingDetails_Changed;
            NumberOfGuestsTextBox.TextChanged += BookingDetails_Changed;
        }

        #endregion

        #region Customer

        /// <summary>
        /// Searches for existing customer.
        /// </summary>
        private void SearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            string searchItem = CustomerSearchTextBox.Text.Trim();

            _selectedCustomer = null;
            CustomerResultsListBox.ItemsSource = null;
            SelectedCustomerTextBlock.Text = "Ingen kund vald.";

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
                MessageBox.Show(
                    $"Kunde inte söka efter kunden eller beräkna rabatten.\n{ex.Message}",
                    "Fel vid kundsökning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens customer registration.
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
                MessageBox.Show(
                    ex.Message,
                    "Kundregistrering",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Accommodation search

        /// <summary>
        /// Resets accommodation search when date or number of guests is changed.
        /// </summary>
        private void BookingDetails_Changed(object sender, EventArgs e)
        {
            InvalidateAccommodationSearch();
        }

        /// <summary>
        /// Clears prior search results and calculated price.
        /// </summary>
        private void InvalidateAccommodationSearch()
        {
            _searchedStartDate = null;
            _searchedEndDate = null;
            _searchedNumberOfGuests = 0;

            _accommodationPrice = 0m;
            _hasCalculatedPrice = false;

            AvailableAccommodationsDataGrid.ItemsSource = null;

            BookingPriceTextBlock.Text =
                "Sök efter lediga boenden och välj ett boende.";
        }

        /// <summary>
        /// Search for available accommodations.
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

            if (numberOfGuests > 6)
            {
                MessageBox.Show(
                    "Största möjliga antalet gäster för ett boende är 6.",
                    "För många gäster",
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
        /// Calculates price when accommodation is chosen.
        /// </summary>
        private void AvailableAccommodations_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
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

                BookingPriceTextBlock.Text =
                    "Kunde inte beräkna priset.";

                MessageBox.Show(
                    ex.Message,
                    "Prisberäkning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Price

        /// <summary>
        /// Updates price when cancellation insurance is chosen or not. 
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
                MessageBox.Show(
                    ex.Message,
                    "Fel vid prisberäkning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Calculate total price including discount and cancellation insurance.
        /// </summary>
        private void UpdateBookingPrice()
        {
            if (!_hasCalculatedPrice)
            {
                BookingPriceTextBlock.Text =
                    "Pris beräknas när boende har valts.";
                return;
            }

            decimal discountAmount = 0m;

            // Calculate discount based on customer type.
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

            string discountText = discountAmount > 0
                ? $"-{discountAmount:N2} kr"
                : "0,00 kr";

            BookingPriceTextBlock.Text =
                $"Boende: {_accommodationPrice:N2} kr\n" +
                $"Rabatt: {discountText}\n" +
                $"Avbeställningsskydd: {insurancePrice:N2} kr\n" +
                $"Totalt: {totalPrice:N2} kr";
        }

        #endregion

        #region Create booking

        /// <summary>
        /// Verifies information regarding booking and allows for creation of booking.
        /// </summary>
        private async void CreateBooking_Click(object sender, RoutedEventArgs e)
        {
            // Verify customer.
            if (_selectedCustomer == null ||
                _selectedCustomer.CustomerID <= 0)
            {
                MessageBox.Show(
                    "Välj en kund innan du skapar bokningen.",
                    "Saknad kund",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Verify dates.
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

            // Verify number of guests. 
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

            // Verify that accommodation is chosen.
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

            // Verify that search and price calculation still applies.
            if (!_hasCalculatedPrice ||
                !_searchedStartDate.HasValue ||
                !_searchedEndDate.HasValue ||
                _searchedNumberOfGuests != numberOfGuests ||
                _searchedStartDate.Value.Date != startDate ||
                _searchedEndDate.Value.Date != endDate)
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

                // Creates booking in database.
                Booking booking = _bookingController.CreateBooking(
                    _selectedCustomer,
                    selectedAccommodation,
                    _searchedStartDate.Value,
                    _searchedEndDate.Value,
                    _searchedNumberOfGuests,
                    hasCancellationInsurance);

                // Shows confirmation that booking with coupled accommodation is created. (Booking requires accommodation)
                MessageBox.Show(
                    $"Nu är fjälläventyret bokat!\nBokningsnummer: {booking.BookingID}\n\nBokningsdetaljerna öppnas så att du kan lägga till utrustning, skidskola och skicka bekräftelse.",
                    "Fjälläventyret är bokat!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Due to BookingDetailsView forcing retreival of booking-object in constructor.
                // ActivatorUtilities is used to build new booking-object. 
                var detailsView = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<BookingDetailsView>(_serviceProvider, booking);
                detailsView.Show();

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

                InvalidateAccommodationSearch();
            }
            finally
            {
                CreateBookingButton.IsEnabled = true;
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Closes window without saving
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
