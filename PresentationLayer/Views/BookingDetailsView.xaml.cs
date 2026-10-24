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

            _serviceProvider = serviceProvider;

            var controller = _serviceProvider.GetRequiredService<BookingController>();
            _booking = controller.GetSpecificBooking(booking.BookingID) ?? booking;
            DataContext = _booking;

            CancelBookingButton.Click += CancelBookingButton_Click;

            UpdateCancelButtonState();
            UpdateVatDisplay();
        }

        /// <summary>
        /// Allows for cancelling of booking and shows status of booking. 
        /// </summary>
        private void UpdateCancelButtonState()
        {
            CancelBookingButton.IsEnabled =
                _booking.BookingStatus != BookingStatus.Cancelled &&
                _booking.BookingStatus != BookingStatus.Finished &&
                _booking.CheckInStatus == CheckInStatus.NotCheckedIn;
        }

        /// <summary>
        /// Handles display of VAT information. 
        /// </summary>
        private void UpdateVatDisplay()
        {
            try
            {
                var controller = _serviceProvider.GetRequiredService<BookingController>();
                var price = controller.BookingPrice(_booking);

                AccomVatRun.Text = price.AccomVat.ToString("N2", CultureInfo.GetCultureInfo("sv-SE")) + " kr";
                OtherVatRun.Text = price.OtherVat.ToString("N2", CultureInfo.GetCultureInfo("sv-SE")) + " kr";
                TotalVatRun.Text = price.TotalVat.ToString("N2", CultureInfo.GetCultureInfo("sv-SE")) + " kr";
            }
            catch
            {
                AccomVatRun.Text = "-";
            }
        }

        /// <summary>
        /// Gets latest data from database to ensure that all coupled data to booking is included. 
        /// </summary>
        private void RefreshBookingDetails()
        {
            try
            {
                var controller = _serviceProvider.GetRequiredService<BookingController>();

                // Get entire booking from database to ensure latest data is included.
                _booking = controller.GetSpecificBooking(_booking.BookingID);

                // Update UI
                DataContext = null;
                DataContext = _booking;
                UpdateVatDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte uppdatera bokningsdetaljerna.\n{ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// nables coupling of Ski School to booking. Uses existing SkiSchoolView.
        /// </summary>
        private void BtnAddSkiSchool_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var skiSchoolView = _serviceProvider.GetRequiredService<SkiSchoolView>();

                skiSchoolView.Owner = this;
                skiSchoolView.ShowDialog();

                // When window is closed, reload data
                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte öppna Skidskolan.\n{ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enables coupling of Equipment to booking. Uses existing EquipmentRentalView
        /// </summary>
        private void BtnAddEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var equipmentView = _serviceProvider.GetRequiredService<EquipmentRentalView>();

                equipmentView.Owner = this;
                equipmentView.ShowDialog();

                // When window is closed, reload of booking details is performed.
                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte öppna utrustningshanteringen.\n{ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enables coupling of Meeting room to booking. Uses DI and ActivatorUtilities (BookingStatusView.xaml.cs) contains fullBooking
        /// </summary>
        private void BtnAddMeetingRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create view using ActivatorUtilities becuase it already accepts IServiceProvider and Booking in constructor.
                var meetingRoomView = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<MeetingRoomBookingView>(
                    _serviceProvider, _booking);

                meetingRoomView.Owner = this;
                meetingRoomView.ShowDialog();

                RefreshBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte öppna mötesrumsvyn.\n{ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async void BtnSendConfirmation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = (System.Windows.Controls.Button)sender;
                btn.IsEnabled = false;

                // Forces update of specific booking (Ensures latest version)
                var controller = _serviceProvider.GetRequiredService<BookingController>();
                Booking fullBooking = controller.GetSpecificBooking(_booking.BookingID);

                // Generate paymentplan
                var paymentPlan = new PaymentPlanService().GeneratePaymentPlan(fullBooking);

                // Send mail (Asynchronous)
                // Ensure that WPF works regarding if mail has been sent or not. 
                await Task.Run(() =>
                {
                    var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                    var emailService = new ServiceLayer.EmailNotificationService(config);

                    emailService.SendEmail(fullBooking.Customer, fullBooking, paymentPlan);
                });

                MessageBox.Show("Bekräftelsen är på väg till kundens inkorg – nu kan fjälläventyret börja!", "Bekräftelse skickad", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte skicka e-post.\n{ex.Message}", "Fel vid utskick", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var btn = (System.Windows.Controls.Button)sender;
                btn.IsEnabled = true;
            }
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
