
using BusinessLayer;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PresentationLayer.Views
{
    public partial class BookingDetailsView : Window
    {
        private Booking _booking;

        public BookingDetailsView(Booking booking)
        {
            InitializeComponent();

            _booking = booking ??
                throw new ArgumentNullException(nameof(booking));

            DataContext = _booking;

            CancelBookingButton.Click += CancelBookingButton_Click;
            EditBookingButton.Click += EditBookingButton_Click;

            UpdateButtonStates();
            UpdateVatDisplay();
        }

        /// <summary>
        /// Updates button availability based on booking status and dates.
        /// </summary>
        private void UpdateButtonStates()
        {
            // Only allow cancellation before check-in.
            CancelBookingButton.IsEnabled =
                _booking.BookingStatus != BookingStatus.Cancelled &&
                _booking.BookingStatus != BookingStatus.Finished &&
                _booking.CheckInStatus == CheckInStatus.NotCheckedIn;

            // Only allow editing before the accommodation stay begins.
            EditBookingButton.IsEnabled =
                (_booking.BookingStatus == BookingStatus.Preliminary ||
                 _booking.BookingStatus == BookingStatus.Confirmed) &&
                _booking.CheckInStatus == CheckInStatus.NotCheckedIn &&
                _booking.Accommodations.Any() &&
                _booking.Accommodations.All(a =>
                    a.StartDate.Date > DateTime.Today);
        }

        /// <summary>
        /// Calculates and displays VAT for the current booking.
        /// </summary>
        private void UpdateVatDisplay()
        {
            try
            {
                var app = (App)Application.Current;

                var controller =
                    app.Services.GetRequiredService<BookingController>();

                var price = controller.BookingPrice(_booking);

                AccomVatRun.Text =
                    price.AccomVat.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo("sv-SE")) + " kr";

                OtherVatRun.Text =
                    price.OtherVat.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo("sv-SE")) + " kr";

                TotalVatRun.Text =
                    price.TotalVat.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo("sv-SE")) + " kr";
            }
            catch
            {
                AccomVatRun.Text = "-";
                OtherVatRun.Text = "-";
                TotalVatRun.Text = "-";
            }
        }

        /// <summary>
        /// Reloads the booking and refreshes the displayed details.
        /// </summary>
        private void RefreshBookingDetails()
        {
            var app = (App)Application.Current;

            var controller =
                app.Services.GetRequiredService<BookingController>();

            // Retrieve the updated booking.
            Booking updatedBooking =
                controller.GetSpecificBooking(_booking.BookingID);

            if (updatedBooking == null)
            {
                throw new InvalidOperationException(
                    "Den uppdaterade bokningen kunde inte hittas.");
            }

            // Replace the current booking with the updated booking.
            _booking = updatedBooking;

            // Refresh all bindings in the details view.
            DataContext = null;
            DataContext = _booking;

            // Recalculate VAT and update button availability.
            UpdateVatDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Cancels a booking and updates the displayed booking status.
        /// </summary>
        private void CancelBookingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var app = (App)Application.Current;

                var controller =
                    app.Services.GetRequiredService<BookingController>();

                // Load the current booking before calculating the refund.
                Booking booking =
                    controller.GetSpecificBooking(_booking.BookingID);

                if (booking == null)
                {
                    throw new InvalidOperationException(
                        "Bokningen kunde inte hittas.");
                }

                decimal? refund =
                    controller.GetCancellationRefund(
                        booking,
                        DateTime.Today);

                string refundInformation;

                if (!booking.HasCancellationInsurance)
                {
                    refundInformation =
                        "Ingen återbetalning gäller. Bokningen saknar " +
                        "avbeställningsskydd och redan betalda belopp återbetalas inte.";
                }
                else if (refund.HasValue)
                {
                    string amount = refund.Value.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo("sv-SE"));

                    refundInformation =
                        $"Beräknat återbetalningsbelopp: {amount} kr.\n\n" +
                        "80 % av bokningsbeloppet återbetalas, exklusive " +
                        "avgiften på 300 kr för avbeställningsskyddet. " +
                        "Skyddet gäller vid avbokning minst fem dagar före ankomst.\n\n" +
                        "Utbetalningen hanteras separat i ekonomisystemet.";
                }
                else
                {
                    refundInformation =
                        "Återbetalningsbeloppet behöver kontrolleras. " +
                        "Den bekräftade återbetalningsregeln kan inte tillämpas " +
                        "på den här bokningen.\n\n" +
                        "Avbokningen genomför inte någon återbetalning.";
                }

                string summary =
                    $"Bokning #{booking.BookingID}\n" +
                    $"{booking.Customer?.DisplayName ?? "Kunduppgift saknas"}";

                var dialog = new CancelBookingView(
                    summary,
                    refundInformation)
                {
                    Owner = this
                };

                // Closing the dialog without confirmation makes no changes.
                if (dialog.ShowDialog() != true)
                    return;

                CancelBookingButton.IsEnabled = false;

                // Save the cancellation.
                controller.CancelBooking(booking.BookingID);

                // Refresh booking details after successful cancellation.
                RefreshBookingDetails();

                MessageBox.Show(
                    "Bokningen har avbokats och finns kvar i kundhistoriken.\n" +
                    "Återbetalning och bekräftelsemejl har inte hanterats.",
                    "Bokning avbokad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateButtonStates();

                MessageBox.Show(
                    $"Kunde inte avboka bokningen.\n{ex.Message}",
                    "Avbokning misslyckades",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the edit booking window and refreshes details after saving.
        /// </summary>
        private void EditBookingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var editWindow = new EditBookingView(_booking)
            {
                Owner = this
            };

            // Only refresh when the user successfully saves changes.
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    RefreshBookingDetails();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ändringen sparades, men bokningsdetaljerna " +
                        "kunde inte uppdateras.\n\n" + ex.Message,
                        "Kunde inte uppdatera vyn",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// Closes the booking details window.
        /// </summary>
        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }

    // Converts booking statuses to Swedish display text.
    public class BookingStatusSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Preliminary" => "Preliminär",
                "Confirmed" => "Bekräftad",
                "Cancelled" => "Avbokad",
                "Canceled" => "Avbokad",
                "Paid" => "Betald",
                _ => value?.ToString() ?? ""
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // Converts accommodation statuses to Swedish display text.
    public class AccommodationStatusSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Available" => "Tillgänglig",
                "Rented" => "Uthyrd",
                "Damaged" => "Skadad",
                "Maintenance" => "Under underhåll",
                _ => value?.ToString() ?? ""
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // Converts boolean values to Swedish Yes or No.
    public class BooleanSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is bool booleanValue
                ? (booleanValue ? "Ja" : "Nej")
                : "";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
