using BusinessLayer;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PresentationLayer.Views
{
    public partial class BookingDetailsView : Window
    {
        private readonly Booking _booking;

        public BookingDetailsView(Booking booking)
        {
            InitializeComponent();

            _booking = booking;
            DataContext = booking;


            CancelBookingButton.Click += CancelBookingButton_Click;

            // Only allow cancellation before check-in.
            CancelBookingButton.IsEnabled =
                booking.BookingStatus != BookingStatus.Cancelled &&
                booking.BookingStatus != BookingStatus.Finished &&
                booking.CheckInStatus == CheckInStatus.NotCheckedIn;
        }

        private void CancelBookingButton_Click(object sender, RoutedEventArgs e)
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

                decimal? refund = controller.GetCancellationRefund(
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

                // Closing or keeping the booking makes no changes.
                if (dialog.ShowDialog() != true)
                    return;

                CancelBookingButton.IsEnabled = false;

                Booking cancelledBooking =
                    controller.CancelBooking(booking.BookingID);

                // Refresh the displayed status after saving succeeds.
                _booking.BookingStatus = cancelledBooking.BookingStatus;
                DataContext = null;
                DataContext = _booking;

                MessageBox.Show(
                    "Bokningen har avbokats och finns kvar i kundhistoriken.\n" +
                    "Återbetalning och bekräftelsemejl har inte hanterats.",
                    "Bokning avbokad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CancelBookingButton.IsEnabled =
                    _booking.BookingStatus != BookingStatus.Cancelled &&
                    _booking.BookingStatus != BookingStatus.Finished &&
                    _booking.CheckInStatus == CheckInStatus.NotCheckedIn;

                MessageBox.Show(
                    $"Kunde inte avboka bokningen.\n{ex.Message}",
                    "Avbokning misslyckades",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
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
