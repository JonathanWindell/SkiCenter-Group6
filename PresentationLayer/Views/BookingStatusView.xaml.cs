using BusinessLayer;
using BusinessLayer.Controllers;
using EntityLayer;
using PresentationLayer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PresentationLayer.Views
{
    public partial class BookingStatusView : Window
    {
        private readonly ObservableCollection<BookingStatusRow> _bookings = new();
        private readonly BookingController _bookingController;

        public BookingStatusView(BookingController bookingController)
        {
            InitializeComponent();

            _bookingController = bookingController;

            CollectionViewSource.GetDefaultView(_bookings).Filter = MatchesFilters;
            BookingsDataGrid.ItemsSource = _bookings;

            BookingStatusFilterComboBox.SelectionChanged += Filters_SelectionChanged;
            PaymentStatusFilterComboBox.SelectionChanged += Filters_SelectionChanged;
            ResetFiltersButton.Click += ResetFiltersButton_Click;

            BookingsDataGrid.SelectionChanged += BookingsDataGrid_SelectionChanged;
            BookingsDataGrid.MouseDoubleClick += BookingsDataGrid_MouseDoubleClick;
            ShowBookingDetailsButton.Click += ShowBookingDetailsButton_Click;

            LoadBookingData();
            PopulateFilters();
            UpdateBookingCount();
        }

        private void LoadBookingData()
        {
            _bookings.Clear();

            var bookingsFromDb = _bookingController.GetAllBookings();

            foreach (var b in bookingsFromDb)
            {
                // Customer names
                string customerName = "Okänd";
                if (b.Customer is PrivateCustomer pc)
                {
                    customerName = $"{pc.FirstName} {pc.LastName}".Trim();
                }
                else if (b.Customer is CorporateCustomer cc)
                {
                    customerName = cc.CompanyName ?? cc.ContactPerson ?? "Företagskund";
                }

                // Gets dates for booking
                DateTime? arrival = b.Accommodations?.Select(a => (DateTime?)a.StartDate)
                    .Concat(b.MeetingRooms?.Select(m => (DateTime?)m.StartDate) ?? Enumerable.Empty<DateTime?>())
                    .Concat(b.Rentals?.Select(r => (DateTime?)r.StartDate) ?? Enumerable.Empty<DateTime?>())
                    .Min();

                DateTime? departure = b.Accommodations?.Select(a => (DateTime?)a.EndDate)
                    .Concat(b.MeetingRooms?.Select(m => (DateTime?)m.EndDate) ?? Enumerable.Empty<DateTime?>())
                    .Concat(b.Rentals?.Select(r => (DateTime?)r.EndDate) ?? Enumerable.Empty<DateTime?>())
                    .Max();

                // Paymentstatus based on Invoices
                string paymentStatus = "Obetald";
                if (b.Invoices != null && b.Invoices.Any())
                {
                    paymentStatus = b.Invoices.All(i => i.Status?.ToString() == "Paid" || i.Status?.ToString() == "Betald")
                        ? "Fullbetald"
                        : "Delbetald";
                }

                _bookings.Add(new BookingStatusRow
                {
                    BookingID = b.BookingID,
                    CustomerName = customerName,
                    ArrivalDate = arrival,
                    DepartureDate = departure,
                    BookingStatus = b.BookingStatus.ToString(),
                    PaymentStatus = paymentStatus
                });
            }

            CollectionViewSource.GetDefaultView(_bookings)?.Refresh();
            UpdateBookingCount();
        }


        private void PopulateFilters()
        {
            foreach (string status in _bookings
                         .Select(booking => booking.BookingStatus)
                         .Distinct()
                         .OrderBy(status => status))
            {
                BookingStatusFilterComboBox.Items.Add(
                    new ComboBoxItem { Content = status });
            }

            foreach (string status in _bookings
                         .Select(booking => booking.PaymentStatus)
                         .Distinct()
                         .OrderBy(status => status))
            {
                PaymentStatusFilterComboBox.Items.Add(
                    new ComboBoxItem { Content = status });
            }
        }

        private bool MatchesFilters(object item)
        {
            if (item is not BookingStatusRow booking)
                return false;

            string? bookingStatus =
                (BookingStatusFilterComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            string? paymentStatus =
                (PaymentStatusFilterComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            // Both selected filters must match the same booking.
            bool matchesBookingStatus =
                BookingStatusFilterComboBox.SelectedIndex <= 0 ||
                booking.BookingStatus == bookingStatus;

            bool matchesPaymentStatus =
                PaymentStatusFilterComboBox.SelectedIndex <= 0 ||
                booking.PaymentStatus == paymentStatus;

            return matchesBookingStatus && matchesPaymentStatus;
        }

        private void Filters_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(_bookings)?.Refresh();
            UpdateBookingCount();

            if (BookingsDataGrid.Items.Count > 0)
            {
                BookingsDataGrid.ScrollIntoView(BookingsDataGrid.Items[0]);
            }
        }

        private void BookingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowBookingDetailsButton.IsEnabled = BookingsDataGrid.SelectedItem is BookingStatusRow;
        }

        private void BookingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BookingsDataGrid.SelectedItem is BookingStatusRow)
            {
                OpenSelectedBookingDetails();
            }
        }

        private void ShowCustomerBookingsButton_Click(object sender, RoutedEventArgs e)
        {
            var customerController = new CustomerController(new DataLayer.UnitOfWork(new DataLayer.SkiCenterDbContext()));
            var customerBookingsView = new CustomerBookingsView(
                                            _bookingController,
                                            customerController,
                                            ((App)System.Windows.Application.Current).Services)
            {
                Owner = this
            };

            bool? result = customerBookingsView.ShowDialog();

            // If booking were added, update the list
            if (result == true || customerBookingsView.HasCreatedAnyBooking)
            {
                LoadBookingData();
            }
        }

        private void ShowBookingDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedBookingDetails();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            BookingStatusFilterComboBox.SelectedIndex = 0;
            PaymentStatusFilterComboBox.SelectedIndex = 0;
        }

        private void OpenSelectedBookingDetails()
        {
            if (BookingsDataGrid.SelectedItem is not BookingStatusRow selectedRow)
            {
                MessageBox.Show("Välj en bokning i listan först.");
                return;
            }

            // Gets the booking with all its contents
            Booking fullBooking = _bookingController.GetSpecificBooking(selectedRow.BookingID);

            if (fullBooking == null)
            {
                MessageBox.Show("Kunde inte hämta bokningsdetaljerna från databasen.",
                                "Fel",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Open the detail window
            var detailsView = new BookingDetailsView(fullBooking)
            {
                Owner = this
            };

            detailsView.ShowDialog();

            // Reload in case of changes when in the view
            LoadBookingData();
        }

        private void UpdateBookingCount()
        {
            var view = CollectionViewSource.GetDefaultView(_bookings);
            int count = view.Cast<BookingStatusRow>().Count();

            BookingCountTextBlock.Text = count switch
            {
                0 => "Inga bokningar matchar filtren.",
                1 => "1 bokning",
                _ => $"{count} bokningar"
            };
        }
    }
}