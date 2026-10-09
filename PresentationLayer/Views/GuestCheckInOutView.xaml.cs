using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PresentationLayer.Models;
using BusinessLayer;
using EntityLayer;

namespace PresentationLayer.Views
{
    public partial class GuestCheckInOutView : Window

    {
        // Uses the existing controller to retrieve booking data.
        private readonly BookingController _bookingController;
        // Preview data only; no database changes are made.
        private readonly ObservableCollection<GuestStayRow> _previewGuests = new();
        private readonly ObservableCollection<GuestStayRow> _visibleGuests = new();

        public GuestCheckInOutView(BookingController bookingController) 
        {
            InitializeComponent();
            _bookingController = bookingController;

            LoadGuests();
            GuestsDataGrid.ItemsSource = _visibleGuests;
            GuestDatePicker.SelectedDate = DateTime.Today;

            GuestDatePicker.SelectedDateChanged += FiltersChanged;
            GuestTypeFilterComboBox.SelectionChanged += FiltersChanged;
            TodayButton.Click += TodayButton_Click;

            GuestsDataGrid.SelectionChanged += GuestsDataGrid_SelectionChanged;

            CheckInButton.Click += CheckInButton_Click;
            CheckOutButton.Click += CheckOutButton_Click;

            RefreshGuests();
        }

        private void GuestsDataGrid_SelectionChanged( object sender, SelectionChangedEventArgs e)
        {
            // Keep actions disabled until saving and permissions are connected.
            CheckInButton.IsEnabled = false;
            CheckOutButton.IsEnabled = false;
            GuestFeedbackBorder.Visibility = Visibility.Collapsed;

            SelectedGuestTextBlock.Text = "Välj en bokning i listan.";
            SelectedGuestDetailsTextBlock.Text = string.Empty;
            GuestPermissionTextBlock.Visibility = Visibility.Collapsed;


            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
            {
                SelectedGuestTextBlock.Text = "Välj en bokning i listan.";
                return;
            }

            SelectedGuestTextBlock.Text =
                  $"{guest.BookingID} – {guest.CustomerName}";

            //Show the accommodation name and the arrival/departure dates for the selected guest
            SelectedGuestDetailsTextBlock.Text =
                $"{guest.CustomerType} · {guest.AccommodationName}\n" +
                $"Ankomst: {guest.ArrivalDate:yyyy-MM-dd} · " + $"Avresa: {guest.DepartureDate:yyyy-MM-dd}";


            // Real user permissions will be supplied by the business layer.
            GuestPermissionTextBlock.Text =
                "In- och utcheckning är ännu inte kopplad till sparande och behörighet.";
            GuestPermissionTextBlock.Visibility = Visibility.Visible;

        }

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
                return;

            // Recheck the preview rules before changing the status.
            if (GuestDatePicker.SelectedDate?.Date != DateTime.Today ||
                guest.ArrivalDate.Date != DateTime.Today ||
                guest.StayStatus != "Ej incheckad" ||
                guest.PaymentStatus != "Fullbetald")
                return;

            // Confirm the guest and accommodation before checking in to reduce mistakes
            MessageBoxResult answer = MessageBox.Show(
                $"Checka in {guest.CustomerName}?\n\n" +
                $"Bokning {guest.BookingID} · {guest.AccommodationName}",
                "Bekräfta incheckning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
                return;

            UpdatePreviewStatus(guest, "Incheckad");
        }

        private void CheckOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
                return;

            if (GuestDatePicker.SelectedDate?.Date != DateTime.Today ||
                guest.DepartureDate.Date != DateTime.Today ||
                guest.StayStatus != "Incheckad")
                return;

            MessageBoxResult answer = MessageBox.Show(
                $"Vill du checka ut {guest.CustomerName}?",
                "Bekräfta utcheckning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
                return;

            UpdatePreviewStatus(guest, "Utcheckad");
        }

        private void UpdatePreviewStatus(GuestStayRow guest, string status)
        {
            // Update the preview table only; nothing is saved to the database.
            guest.StayStatus = status;
            GuestsDataGrid.Items.Refresh();

            CheckInButton.IsEnabled = false;
            CheckOutButton.IsEnabled = false;
            GuestFeedbackBorder.Visibility = Visibility.Collapsed;

            SelectedGuestTextBlock.Text =
                $"Bokning {guest.BookingID} – {status.ToLowerInvariant()} i testvyn.";
        }

        private void LoadGuests()
        {
            _previewGuests.Clear();

            try
            {
                // Load bookings through the existing business controller.
                var bookings = _bookingController.GetAllBookings();

                foreach (Booking booking in bookings)
                {
                    if (booking.BookingStatus == BookingStatus.Cancelled)
                        continue;

                    foreach (BookingAccommodation stay in booking.Accommodations)
                    {
                        _previewGuests.Add(new GuestStayRow
                        {
                            BookingID = booking.BookingID,
                            BookingAccommodationID = stay.BookingAccommodationID,
                            CustomerName =
                                booking.Customer?.DisplayName ?? "Kunduppgift saknas",
                            CustomerType = booking.Customer == null
                                ? "Okänd"
                                : booking.Customer is CorporateCustomer
                                    ? "Företagskund"
                                    : "Privatkund",
                            AccommodationName = stay.Accommodation?.AccommodationNumber ?? "Boendeuppgift saknas",
                            ArrivalDate = stay.StartDate,
                            DepartureDate = stay.EndDate,

                            // Payment rules have not been connected yet.
                            PaymentStatus = "Ej tillgänglig",

                            StayStatus = booking.CheckInStatus switch
                            {
                                CheckInStatus.NotCheckedIn => "Ej incheckad",
                                CheckInStatus.CheckedIn => "Incheckad",
                                CheckInStatus.CheckedOut => "Utcheckad",
                                _ => "Okänd"
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
               
                _previewGuests.Clear();

                MessageBox.Show(
                    "Bokningarna kunde inte hämtas. Kontrollera databasanslutningen.",
                    "Kunde inte läsa bokningar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FiltersChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshGuests();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            GuestDatePicker.SelectedDate = DateTime.Today;
        }

        private void RefreshGuests()
        {
            _visibleGuests.Clear(); //holds only the guests that matches the filter

            CheckInButton.IsEnabled = false;
            CheckOutButton.IsEnabled = false;
            GuestFeedbackBorder.Visibility = Visibility.Collapsed;
            SelectedGuestTextBlock.Text = "Välj en bokning i listan.";
            SelectedGuestDetailsTextBlock.Text = string.Empty;

            if (GuestDatePicker.SelectedDate is not DateTime selectedDate)
            {
                GuestCountTextBlock.Text = "Välj ett datum.";
                return;
            }

            string filter =
                (GuestTypeFilterComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "Alla";

            foreach (GuestStayRow guest in _previewGuests)
            {
                bool isArrival = guest.ArrivalDate.Date == selectedDate.Date;
                bool isDeparture = guest.DepartureDate.Date == selectedDate.Date;

                if (!isArrival && !isDeparture)
                    continue;

                if (filter == "Ankomster" && !isArrival)
                    continue;

                if (filter == "Avresor" && !isDeparture)
                    continue;

                // A stay may have both events on the same date.
                guest.EventType = filter == "Ankomster" ? "Ankomst"
                    : filter == "Avresor" ? "Avresa"
                    : isArrival && isDeparture ? "Ankomst / avresa"
                    : isArrival ? "Ankomst" : "Avresa";

                _visibleGuests.Add(guest);
            }

            int bookingCount = _visibleGuests
                .Select(guest => guest.BookingID)
                .Distinct()
                .Count();

            GuestCountTextBlock.Text = bookingCount == 0
                ? "Inga ankomster eller avresor för valt datum."
                : $"{bookingCount} bokningar";
        }
    }
}