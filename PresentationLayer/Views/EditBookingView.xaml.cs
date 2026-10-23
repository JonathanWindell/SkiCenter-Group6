
using BusinessLayer;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;

namespace PresentationLayer.Views
{
    public partial class EditBookingView : Window
    {
        private readonly Booking _booking;
        private readonly BookingController _bookingController;

        public EditBookingView(Booking booking)
        {
            InitializeComponent();

            _booking = booking
                ?? throw new ArgumentNullException(nameof(booking));

            var app = (App)Application.Current;

            _bookingController =
                app.Services.GetRequiredService<BookingController>();

            BookingNumberText.Text =
                $"Bokning #{_booking.BookingID}";

            var accommodation =
                _booking.Accommodations.FirstOrDefault();

            if (accommodation != null)
            {
                StartDatePicker.SelectedDate =
                    accommodation.StartDate;

                EndDatePicker.SelectedDate =
                    accommodation.EndDate;
            }

            // Guest count is not stored on Booking.
            // Staff must enter the number of guests.
            NumberOfGuestsTextBox.Text = "";

            // Connect buttons.
            SearchAccommodationButton.Click +=
                SearchAccommodationButton_Click;

            SaveChangesButton.Click +=
                SaveChangesButton_Click;

            CancelButton.Click +=
                CancelButton_Click;

            // Enable saving only after selecting an accommodation.
            AccommodationDataGrid.SelectionChanged += (_, _) =>
            {
                SaveChangesButton.IsEnabled =
                    AccommodationDataGrid.SelectedItem is Accommodation;
            };

            // Require a new search when search criteria change.
            StartDatePicker.SelectedDateChanged +=
                (_, _) => ClearSearch();

            EndDatePicker.SelectedDateChanged +=
                (_, _) => ClearSearch();

            NumberOfGuestsTextBox.TextChanged +=
                (_, _) => ClearSearch();

            SaveChangesButton.IsEnabled = false;
        }

        private void ClearSearch()
        {
            if (AccommodationDataGrid == null ||
                SaveChangesButton == null)
            {
                return;
            }

            AccommodationDataGrid.ItemsSource = null;
            AccommodationDataGrid.SelectedItem = null;
            SaveChangesButton.IsEnabled = false;
        }

        private bool ValidateForm()
        {
            if (!StartDatePicker.SelectedDate.HasValue)
            {
                ShowWarning(
                    "Du måste ange ett incheckningsdatum.");

                StartDatePicker.Focus();
                return false;
            }

            if (!EndDatePicker.SelectedDate.HasValue)
            {
                ShowWarning(
                    "Du måste ange ett utcheckningsdatum.");

                EndDatePicker.Focus();
                return false;
            }

            DateTime startDate =
                StartDatePicker.SelectedDate.Value.Date;

            DateTime endDate =
                EndDatePicker.SelectedDate.Value.Date;

            if (startDate <= DateTime.Today)
            {
                ShowWarning(
                    "Incheckningsdatum måste vara efter dagens datum.");

                StartDatePicker.Focus();
                return false;
            }

            if (endDate <= startDate)
            {
                ShowWarning(
                    "Utcheckningsdatum måste vara senare än incheckningsdatum.");

                EndDatePicker.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                NumberOfGuestsTextBox.Text))
            {
                ShowWarning(
                    "Du måste ange antal gäster.");

                NumberOfGuestsTextBox.Focus();
                return false;
            }

            if (!int.TryParse(
                    NumberOfGuestsTextBox.Text,
                    out int guests) ||
                guests <= 0)
            {
                ShowWarning(
                    "Antal gäster måste vara ett heltal större än 0.");

                NumberOfGuestsTextBox.Focus();
                return false;
            }

            if ((_booking.BookingStatus != BookingStatus.Preliminary &&
                 _booking.BookingStatus != BookingStatus.Confirmed) ||
                _booking.CheckInStatus != CheckInStatus.NotCheckedIn ||
                !_booking.Accommodations.Any() ||
                _booking.Accommodations.Any(a =>
                    a.StartDate.Date <= DateTime.Today))
            {
                ShowWarning(
                    "Den här bokningen kan inte ändras eftersom den " +
                    "är påbörjad, avslutad, avbokad eller incheckad.");

                return false;
            }

            // Additional services may be tied to the original stay dates.
            bool hasAdditionalServices =
                _booking.Rentals?.Any() == true ||
                _booking.SkiLessons?.Any() == true ||
                _booking.MeetingRooms?.Any() == true;

            if (hasAdditionalServices)
            {
                var originalStay =
                    _booking.Accommodations.First();

                if (startDate != originalStay.StartDate.Date ||
                    endDate != originalStay.EndDate.Date)
                {
                    ShowWarning(
                        "Bokningen innehåller andra tjänster, till exempel " +
                        "skidskola eller uthyrning.\n\n" +
                        "Du kan byta boende, men måste behålla samma datum.\n\n" +
                        "Ändra eller avboka de andra tjänsterna separat " +
                        "innan du ändrar vistelseperioden.");

                    return false;
                }
            }

            return true;
        }

        private void SearchAccommodationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                DateTime startDate =
                    StartDatePicker.SelectedDate!.Value.Date;

                DateTime endDate =
                    EndDatePicker.SelectedDate!.Value.Date;

                int guests =
                    int.Parse(NumberOfGuestsTextBox.Text);

                ClearSearch();

                var accommodations = _bookingController
                    .GetAvailableAccommodationsForEdit(
                        startDate,
                        endDate,
                        guests,
                        _booking.BookingID)
                    .ToList();

                AccommodationDataGrid.ItemsSource =
                    accommodations;

                if (accommodations.Count == 0)
                {
                    ShowWarning(
                        "Inga lediga boenden hittades för de valda " +
                        "datumen och antalet gäster.");

                    return;
                }

                MessageBox.Show(
                    $"{accommodations.Count} lediga boenden hittades.\n\n" +
                    "Välj ett boende i listan.",
                    "Sökresultat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Det gick inte att söka efter lediga boenden.\n\n" +
                    ex.Message,
                    "Fel vid sökning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveChangesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            if (AccommodationDataGrid.SelectedItem
                is not Accommodation selectedAccommodation)
            {
                ShowWarning(
                    "Du måste välja ett boende innan du sparar.");

                return;
            }

            int guests =
                int.Parse(NumberOfGuestsTextBox.Text);

            if (selectedAccommodation.AccommodationType == null ||
                selectedAccommodation.AccommodationType.NumberOfBeds < guests)
            {
                ShowWarning(
                    "Det valda boendet har inte tillräckligt många bäddar.");

                return;
            }

            DateTime newStartDate =
                StartDatePicker.SelectedDate!.Value.Date;

            DateTime newEndDate =
                EndDatePicker.SelectedDate!.Value.Date;

            var oldStay =
                _booking.Accommodations.FirstOrDefault();

            if (oldStay == null)
            {
                ShowWarning(
                    "Bokningens tidigare boende kunde inte hittas.");

                return;
            }

            string oldAccommodationNumber =
                oldStay.Accommodation?.AccommodationNumber
                ?? "Okänt boende";

            string newAccommodationNumber =
                selectedAccommodation.AccommodationNumber;

            DateTime oldStartDate =
                oldStay.StartDate;

            DateTime oldEndDate =
                oldStay.EndDate;

            decimal oldTotalAmount =
                _booking.TotalAmount;

            try
            {
                // Check whether the accommodation and dates are unchanged.
                bool isUnchanged =
                    oldStay.AccommodationID ==
                        selectedAccommodation.AccommodationID &&
                    oldStay.StartDate.Date == newStartDate &&
                    oldStay.EndDate.Date == newEndDate;


                // Preserve the saved accommodation price if nothing changed.
                decimal newListPrice = isUnchanged
                    ? _booking.AccommodationListPrice
                    : _bookingController.CalculateAccommodationPrice(
                        selectedAccommodation.AccommodationTypeID,
                        newStartDate,
                        newEndDate);

                decimal rate = _booking.DiscountRate;

                if (rate < 0m || rate > 1m)
                {
                    ShowWarning(
                        "Bokningens rabattprocent är ogiltig.");

                    return;
                }

                // Preserve the saved discount if nothing changed.
                decimal newDiscount = isUnchanged
                    ? _booking.DiscountAmount
                    : Math.Round(
                        newListPrice * rate,
                        2,
                        MidpointRounding.AwayFromZero);

                // Verify the original accommodation price.
                if (_booking.AccommodationListPrice <= 0m ||
                    _booking.DiscountAmount < 0m ||
                    _booking.DiscountAmount >
                        _booking.AccommodationListPrice)
                {
                    ShowWarning(
                        "Bokningens ursprungliga boendepris saknas " +
                        "eller är ogiltigt.\n\n" +
                        "Priset måste kontrolleras innan bokningen kan ändras.");

                    return;
                }

                // Calculate the old accommodation price after discount.
                decimal oldAccommodationPrice =
                    _booking.AccommodationListPrice -
                    _booking.DiscountAmount;

                // Calculate the new accommodation price after discount.
                decimal newAccommodationPrice =
                    newListPrice - newDiscount;

                // Preserve all existing additional services and insurance.
                decimal estimatedTotal =
                    oldTotalAmount -
                    oldAccommodationPrice +
                    newAccommodationPrice;

                if (estimatedTotal < 0m)
                {
                    ShowWarning(
                        "Det nya totalpriset kunde inte beräknas.");

                    return;
                }

                string confirmationMessage =
                    $"Vill du spara följande ändringar?\n\n" +
                    $"Bokning: #{_booking.BookingID}\n\n" +
                    $"TIDIGARE\n" +
                    $"Boende: {oldAccommodationNumber}\n" +
                    $"Incheckning: {oldStartDate:yyyy-MM-dd}\n" +
                    $"Utcheckning: {oldEndDate:yyyy-MM-dd}\n" +
                    $"Totalt pris: {oldTotalAmount:N2} kr\n\n" +
                    $"NYTT\n" +
                    $"Boende: {newAccommodationNumber}\n" +
                    $"Incheckning: {newStartDate:yyyy-MM-dd}\n" +
                    $"Utcheckning: {newEndDate:yyyy-MM-dd}\n" +
                    $"Totalt pris: {estimatedTotal:N2} kr";

                MessageBoxResult confirmation =
                    MessageBox.Show(
                        confirmationMessage,
                        "Bekräfta ändring av bokning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                {
                    return;
                }

                // Save changes to the existing booking.
                Booking updatedBooking =
                    _bookingController.UpdateBookingAccommodation(
                        _booking.BookingID,
                        selectedAccommodation.AccommodationID,
                        newStartDate,
                        newEndDate,
                        guests);

                MessageBox.Show(
                    $"Bokning #{updatedBooking.BookingID} har uppdaterats!\n\n" +
                    $"Boende: {newAccommodationNumber}\n" +
                    $"Incheckning: {newStartDate:yyyy-MM-dd}\n" +
                    $"Utcheckning: {newEndDate:yyyy-MM-dd}\n" +
                    $"Nytt totalpris: {updatedBooking.TotalAmount:N2} kr",
                    "Bokningen har uppdaterats",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Det gick inte att spara ändringarna.\n\n" +
                    ex.Message,
                    "Fel vid sparning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                "Kontrollera uppgifterna",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
