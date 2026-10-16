using BusinessLayer;
using EntityLayer;
using PresentationLayer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PresentationLayer.Views
{
    public partial class GuestCheckInOutView : Window

    {
        // Uses the existing controller to retrieve booking data.
        private readonly BookingController _bookingController;

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

        private void GuestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckInButton.IsEnabled = false;
            CheckOutButton.IsEnabled = false;
            GuestFeedbackBorder.Visibility = Visibility.Collapsed;
            GuestPermissionTextBlock.Visibility = Visibility.Collapsed;

            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
            {
                SelectedGuestTextBlock.Text = "Välj en bokning i listan.";
                SelectedGuestDetailsTextBlock.Text = string.Empty;
                return;
            }

            SelectedGuestTextBlock.Text = $"{guest.BookingID} – {guest.CustomerName}";
            SelectedGuestDetailsTextBlock.Text =
                $"{guest.CustomerType} · {guest.AccommodationName}\n" +
                $"Ankomst: {guest.ArrivalDate:yyyy-MM-dd} · Avresa: {guest.DepartureDate:yyyy-MM-dd}";

            DateTime selectedDate = GuestDatePicker.SelectedDate?.Date ?? DateTime.Today;

            bool isArrivalDay = guest.ArrivalDate.Date == selectedDate;

            // Check in. Guest can be checked in if it¨s arrival day or if status is Not checked in
            bool canCheckIn = (isArrivalDay && guest.StayStatus == "Ej incheckad") || (guest.StayStatus == "Utcheckad");
            bool isPaid = true; // Placeholder until Invoice is created and integrated.

            if (canCheckIn)
            {
                if (!isPaid)
                {
                    GuestFeedbackTextBlock.Text = "Bokningen är inte fullbetald. Gästen kan inte checkas in.";
                    GuestFeedbackBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    CheckInButton.IsEnabled = true;
                }
            }

            // Check out. All guests that have status "Checked In" should be able to be checked out. 
            if (guest.StayStatus == "Incheckad")
            {
                CheckOutButton.IsEnabled = true;
            }
        }

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
                return;

            // Changes the question depending in if it's the first time checking in or if precious mistakes has been made. 
            string message = guest.StayStatus == "Utcheckad"
                ? $"Gästen är redan utcheckad. Vill du checka in {guest.CustomerName} igen?\n\nBokning {guest.BookingID} · {guest.AccommodationName}"
                : $"Checka in {guest.CustomerName}?\n\nBokning {guest.BookingID} · {guest.AccommodationName}";

            MessageBoxResult answer = MessageBox.Show(
                message,
                "Bekräfta incheckning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            // Forces database update
            bool success = _bookingController.UpdateCheckInStatus(guest.BookingID, CheckInStatus.CheckedIn);

            if (success)
            {
                MessageBox.Show($"Bokning #{guest.BookingID} ({guest.CustomerName}) är nu incheckad!", "Incheckad", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGuests();
                RefreshGuests();
            }
            else
            {
                MessageBox.Show("Kunde inte genomföra incheckningen i databasen.", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
                return;

            // Verify that Guest is checked in
            if (guest.StayStatus != "Incheckad")
                return;

            MessageBoxResult answer = MessageBox.Show(
                $"Vill du checka ut {guest.CustomerName}?\n\n" +
                $"Bokning {guest.BookingID} · {guest.AccommodationName}",
                "Bekräfta utcheckning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            // Forces update against database.
            bool success = _bookingController.UpdateCheckInStatus(guest.BookingID, CheckInStatus.CheckedOut);

            if (success)
            {
                MessageBox.Show($"Bokning #{guest.BookingID} ({guest.CustomerName}) är nu utcheckad!", "Utcheckad", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGuests();
                RefreshGuests();
            }
            else
            {
                MessageBox.Show("Kunde inte genomföra utcheckningen i databasen.", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            _visibleGuests.Clear(); // holds only the guests that matches the filter

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

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Show();
            }
            this.Close();
        }
    }
}