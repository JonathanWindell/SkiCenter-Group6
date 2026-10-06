using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessLayer;
using EntityLayer;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for CustomerBookingsView.xaml
    /// </summary>
    public partial class CustomerBookingsView : Window
    {
        private readonly BookingController _bookingController;

        public CustomerBookingsView(BookingController bookingController)
        {
            InitializeComponent();

            _bookingController = bookingController;
        }

        /// <summary>
        /// Loads all bookings belonging to the selected customer.
        /// </summary>
        /// <param name="customerId">The ID of the selected customer.</param>
        public void LoadCustomerBookings(int customerId)
        {
            List<Booking> bookings = _bookingController
                .GetBookingsByCustomer(customerId)
                .ToList();

            BookingsItemsControl.ItemsSource = bookings;

            if (bookings.Count == 0)
            {
                NoBookingsMessage.Visibility = Visibility.Visible;
                BookingsItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoBookingsMessage.Visibility = Visibility.Collapsed;
                BookingsItemsControl.Visibility = Visibility.Visible;
            }
        }

        // Opens the detailed view for the selected booking.
        private void ShowAllInformation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Booking booking)
            {
                BookingDetailsView bookingDetailsView =
                    new BookingDetailsView(booking);

                bookingDetailsView.ShowDialog();
            }
        }

        // Closes the booking overview and returns to the previous view.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}