using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PresentationLayer.Models;

namespace PresentationLayer.Views
{
    public partial class GuestCheckInOutView : Window
    {
        // Preview data only; no database changes are made.
        private readonly ObservableCollection<GuestStayRow> _previewGuests = new();
        private readonly ObservableCollection<GuestStayRow> _visibleGuests = new();

        public GuestCheckInOutView() //runs when window is opned, loads the guests, selects today and connects the controls to their methods
        {
            InitializeComponent();

            AddPreviewGuests();
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
            CheckInButton.IsEnabled = false;
            CheckOutButton.IsEnabled = false;
            GuestFeedbackBorder.Visibility = Visibility.Collapsed;

            if (GuestsDataGrid.SelectedItem is not GuestStayRow guest)
            {
                SelectedGuestTextBlock.Text = "Välj en bokning i listan.";
                return;
            }

            SelectedGuestTextBlock.Text =
                $"Bokning {guest.BookingID} – {guest.CustomerName}";

            //Show the accommodation name and the arrival/departure dates for the selected guest
            SelectedGuestDetailsTextBlock.Text =
                $"{guest.CustomerType} · {guest.AccommodationName}\n" +
                $"Ankomst: {guest.ArrivalDate:yyyy-MM-dd} · " + $"Avresa: {guest.DepartureDate:yyyy-MM-dd}";


            // Real user permissions will be supplied by the business layer.
            GuestPermissionTextBlock.Text =
                "Testvy: användarbehörighet är ännu inte ansluten.";
            GuestPermissionTextBlock.Visibility = Visibility.Visible;

            // Preview actions are limited to today's arrivals and departures.
            if (GuestDatePicker.SelectedDate?.Date != DateTime.Today)
                return;

            bool canCheckIn =
                guest.ArrivalDate.Date == DateTime.Today &&
                guest.StayStatus == "Ej incheckad";

            bool hasUnpaidBalance = guest.PaymentStatus != "Fullbetald";

            CheckInButton.IsEnabled = canCheckIn && !hasUnpaidBalance;

            CheckOutButton.IsEnabled =
                guest.DepartureDate.Date == DateTime.Today &&
                guest.StayStatus == "Incheckad";

            if (canCheckIn && hasUnpaidBalance)
            {
                GuestFeedbackTextBlock.Text =
                    "Bokningen är inte fullbetald. Gästen kan inte checkas in.";
                GuestFeedbackBorder.Visibility = Visibility.Visible;
            }
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

        private void AddPreviewGuests() // Will hold all our 'fake' guest data
        {
            DateTime today = DateTime.Today;

            _previewGuests.Add(new GuestStayRow
            {
                BookingID = 2031,
                BookingAccommodationID = 1,
                CustomerName = "Anna Andersson",
                CustomerType = "Privatkund",
                AccommodationName = "Stuga 12",
                ArrivalDate = today,
                DepartureDate = today.AddDays(7),
                PaymentStatus = "Fullbetald",
                StayStatus = "Ej incheckad"
            });

            _previewGuests.Add(new GuestStayRow
            {
                BookingID = 2032,
                BookingAccommodationID = 2,
                CustomerName = "Fjällteam AB",
                CustomerType = "Företagskund",
                AccommodationName = "Lägenhet 4",
                ArrivalDate = today,
                DepartureDate = today.AddDays(5),
                PaymentStatus = "Obetald",
                StayStatus = "Ej incheckad"
            });

            _previewGuests.Add(new GuestStayRow
            {
                BookingID = 2033,
                BookingAccommodationID = 3,
                CustomerName = "Erik Svensson",
                CustomerType = "Privatkund",
                AccommodationName = "Stuga 8",
                ArrivalDate = today.AddDays(-7),
                DepartureDate = today,
                PaymentStatus = "Fullbetald",
                StayStatus = "Incheckad"
            });

            _previewGuests.Add(new GuestStayRow
            {
                BookingID = 2034,
                BookingAccommodationID = 4,
                CustomerName = "Sara Nilsson",
                CustomerType = "Privatkund",
                AccommodationName = "Lägenhet 2",
                ArrivalDate = today.AddDays(-5),
                DepartureDate = today,
                PaymentStatus = "Fullbetald",
                StayStatus = "Utcheckad"
            });
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