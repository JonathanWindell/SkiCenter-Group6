using PresentationLayer.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PresentationLayer.Views
{
    public partial class BookingStatusView : Window
    {
        private readonly ObservableCollection<BookingStatusRow> _bookings = new();
        private readonly ICollectionView _bookingView;

        public BookingStatusView()
        {
            InitializeComponent();

            // Preview data only. Replace with data from the business layer later.
            AddPreviewBookings();

            _bookingView = new ListCollectionView(_bookings);
            _bookingView.Filter = MatchesFilters;
            BookingsDataGrid.ItemsSource = _bookingView;

            // Use display values present in the supplied rows.
            PopulateFilters();

            BookingStatusFilterComboBox.SelectionChanged += Filters_SelectionChanged;
            PaymentStatusFilterComboBox.SelectionChanged += Filters_SelectionChanged;
            ResetFiltersButton.Click += ResetFiltersButton_Click;

            UpdateBookingCount();
        }

        private void AddPreviewBookings()
        {
            _bookings.Add(new BookingStatusRow
            {
                BookingID = 2031,
                CustomerName = "Anna Andersson",
                ArrivalDate = new DateTime(2027, 1, 24),
                DepartureDate = new DateTime(2027, 1, 31),
                BookingStatus = "Bekräftad",
                PaymentStatus = "Fullbetald"
            });

            _bookings.Add(new BookingStatusRow
            {
                BookingID = 2032,
                CustomerName = "Fjällteam AB",
                ArrivalDate = new DateTime(2027, 2, 1),
                DepartureDate = new DateTime(2027, 2, 7),
                BookingStatus = "Preliminär",
                PaymentStatus = "Obetald"
            });

            _bookings.Add(new BookingStatusRow
            {
                BookingID = 2033,
                CustomerName = "Erik Svensson",
                ArrivalDate = new DateTime(2027, 2, 10),
                DepartureDate = new DateTime(2027, 2, 14),
                BookingStatus = "Bekräftad",
                PaymentStatus = "Delbetald"
            });

            _bookings.Add(new BookingStatusRow
            {
                BookingID = 2034,
                CustomerName = "Sara Nilsson",
                ArrivalDate = new DateTime(2027, 2, 15),
                DepartureDate = new DateTime(2027, 2, 20),
                BookingStatus = "Avbokad",
                PaymentStatus = "Obetald"
            });

            _bookings.Add(new BookingStatusRow
            {
                BookingID = 2035,
                CustomerName = "Lina Berg",
                ArrivalDate = null,
                DepartureDate = null,
                BookingStatus = "Bekräftad",
                PaymentStatus = "Obetald"
            });
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
            _bookingView.Refresh();
            UpdateBookingCount();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            BookingStatusFilterComboBox.SelectedIndex = 0;
            PaymentStatusFilterComboBox.SelectedIndex = 0;
        }

        private void UpdateBookingCount()
        {
            int count = _bookingView.Cast<BookingStatusRow>().Count();

            BookingCountTextBlock.Text = count switch
            {
                0 => "Inga bokningar matchar filtren.",
                1 => "1 bokning",
                _ => $"{count} bokningar"
            };
        }
    }
}