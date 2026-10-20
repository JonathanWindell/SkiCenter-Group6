
using BusinessLayer;
using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
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

            // Knappen ska vara klickbar så att felmeddelanden kan visas.
            CreateBookingButton.IsEnabled = true;

            // Nollställ boendesökningen om bokningsuppgifter ändras.
            StartDatePicker.SelectedDateChanged += BookingDetails_Changed;
            EndDatePicker.SelectedDateChanged += BookingDetails_Changed;
            NumberOfGuestsTextBox.TextChanged += BookingDetails_Changed;
        }

        /// <summary>
        /// Söker efter en befintlig kund.
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
        /// Öppnar kundregistreringen.
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

        /// <summary>
        /// Nollställer boendesökningen när datum eller gästantal ändras.
        /// </summary>
        private void BookingDetails_Changed(object sender, EventArgs e)
        {
            InvalidateAccommodationSearch();
        }

        /// <summary>
        /// Rensar tidigare sökresultat och beräknat pris.
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

            // Inaktivera inte knappen här.
            // Fel kontrolleras när användaren klickar på Skapa bokning.
        }

        /// <summary>
        /// Söker efter lediga boenden.
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
        /// Beräknar priset när ett boende väljs.
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

        /// <summary>
        /// Uppdaterar priset när avbeställningsskydd ändras.
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
        /// Beräknar totalpris inklusive rabatt och avbeställningsskydd.
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

            // Beräkna rabatt beroende på kundtyp.
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

        /// <summary>
        /// Kontrollerar uppgifter och skapar bokningen.
        /// </summary>
        private void CreateBooking_Click(object sender, RoutedEventArgs e)
        {
            // Kontrollera kund först.
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

            // Kontrollera datum.
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

            // Kontrollera antal gäster.
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

            // Kontrollera att boende har valts.
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

            // Kontrollera att sökning och prisberäkning fortfarande gäller.
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
                // Inaktivera knappen tillfälligt under sparandet.
                CreateBookingButton.IsEnabled = false;

                // Controller kontrollerar tillgänglighet och räknar om priset.
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

                // Kräver en ny sökning om sparandet misslyckades.
                InvalidateAccommodationSearch();
            }
            finally
            {
                // Om fönstret fortfarande är öppet kan användaren försöka igen.
                CreateBookingButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Stänger fönstret utan att spara.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
