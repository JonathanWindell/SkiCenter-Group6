
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
        private Booking _booking;
        private readonly IServiceProvider _serviceProvider;

        public BookingDetailsView(IServiceProvider serviceProvider, Booking booking)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            if (booking == null) throw new ArgumentNullException(nameof(booking));

            var controller = _serviceProvider.GetRequiredService<BookingController>();
            _booking = controller.GetSpecificBooking(booking.BookingID) ?? booking;

            DataContext = _booking;

            CancelBookingButton.Click += CancelBookingButton_Click;
            EditBookingButton.Click += EditBookingButton_Click;

            UpdateButtonStates();
            UpdateVatDisplay();
        }

        /// <summary>
        /// Updates cancellation and editing button availability.
        /// </summary>
        private void UpdateButtonStates()
        {
            // Cancellation is allowed before check-in,
            // unless the booking is cancelled or finished.
            CancelBookingButton.IsEnabled =
                _booking.BookingStatus != BookingStatus.Cancelled &&
                _booking.BookingStatus != BookingStatus.Finished &&
                _booking.CheckInStatus == CheckInStatus.NotCheckedIn;

            // Editing is only allowed for future accommodation stays.
            EditBookingButton.IsEnabled =
                (_booking.BookingStatus == BookingStatus.Preliminary ||
                 _booking.BookingStatus == BookingStatus.Confirmed) &&
                _booking.CheckInStatus == CheckInStatus.NotCheckedIn &&
                _booking.Accommodations != null &&
                _booking.Accommodations.Any() &&
                _booking.Accommodations.All(a =>
                    a.StartDate.Date > DateTime.Today);
        }

        /// <summary>
        /// Calculates and displays VAT for the booking.
        /// </summary>
        private void UpdateVatDisplay()
        {
            try
            {
                var controller =
                    _serviceProvider.GetRequiredService<BookingController>();

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
        /// Reloads the booking from the database and refreshes the view.
        /// </summary>
        private void RefreshBookingDetails()
        {
            var controller =
                _serviceProvider.GetRequiredService<BookingController>();

            Booking updatedBooking =
                controller.GetSpecificBooking(_booking.BookingID);

            if (updatedBooking == null)
            {
                throw new InvalidOperationException(
                    "Den uppdaterade bokningen kunde inte hittas.");
            }

            _booking = updatedBooking;

            DataContext = null;
            DataContext = _booking;

            UpdateVatDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Opens the ski school window.
        /// </summary>
        private void BtnAddSkiSchool_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var skiSchoolView =
                    _serviceProvider.GetRequiredService<SkiSchoolView>();

                skiSchoolView.Owner = this;
                skiSchoolView.ShowDialog();

                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte öppna Skidskolan.\n{ex.Message}",
                    "Fel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the equipment rental window.
        /// </summary>
        private void BtnAddEquipment_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var equipmentView =
                    _serviceProvider.GetRequiredService<EquipmentRentalView>();

                equipmentView.Owner = this;
                equipmentView.ShowDialog();

                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte öppna utrustningshanteringen.\n{ex.Message}",
                    "Fel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the meeting room booking window.
        /// </summary>
        private void BtnAddMeetingRoom_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var meetingRoomView =
                    ActivatorUtilities.CreateInstance<MeetingRoomBookingView>(
                        _serviceProvider,
                        _booking);

                meetingRoomView.Owner = this;
                meetingRoomView.ShowDialog();

                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte öppna mötesrumsvyn.\n{ex.Message}",
                    "Fel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sends the booking confirmation by email.
        /// </summary>
        private async void BtnSendConfirmation_Click(
            object sender,
            RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.Button)sender;

            try
            {
                btn.IsEnabled = false;

                var controller =
                    _serviceProvider.GetRequiredService<BookingController>();

                Booking fullBooking =
                    controller.GetSpecificBooking(_booking.BookingID);

                if (fullBooking == null)
                {
                    throw new InvalidOperationException(
                        "Bokningen kunde inte hittas.");
                }

                var paymentPlan =
                    new PaymentPlanService().GeneratePaymentPlan(fullBooking);

                await Task.Run(() =>
                {
                    var config =
                        _serviceProvider.GetRequiredService<
                            Microsoft.Extensions.Configuration.IConfiguration>();

                    var emailService =
                        new ServiceLayer.EmailNotificationService(config);

                    emailService.SendEmail(
                        fullBooking.Customer,
                        fullBooking,
                        paymentPlan);
                });

                MessageBox.Show(
                    "Bekräftelsen är på väg till kundens inkorg – nu kan fjälläventyret börja!",
                    "Bekräftelse skickad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte skicka e-post.\n{ex.Message}",
                    "Fel vid utskick",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Cancels a booking and updates its displayed status.
        /// </summary>
        private void CancelBookingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var controller =
                    _serviceProvider.GetRequiredService<BookingController>();

                // Load the latest booking before calculating the refund.
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

                // Closing without confirmation makes no changes.
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                CancelBookingButton.IsEnabled = false;

                // Save the cancellation.
                controller.CancelBooking(booking.BookingID);

                // Refresh the displayed details.
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
        /// Opens the edit booking window and refreshes after saving.
        /// </summary>
        private void EditBookingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var editWindow = new EditBookingView(_booking)
                {
                    Owner = this
                };

                // Only refresh after a successful save.
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte öppna Ändra bokning.\n{ex.Message}",
                    "Fel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
