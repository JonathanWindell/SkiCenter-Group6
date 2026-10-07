using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessLayer;
using BusinessLayer.Controllers;
using EntityLayer;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for CustomerBookingsView.xaml
    /// </summary>
    public partial class CustomerBookingsView : Window
    {
        private readonly BookingController _bookingController;
        private readonly CustomerController _customerController;

        public CustomerBookingsView(
            BookingController bookingController,
            CustomerController customerController)
        {
            InitializeComponent();

            _bookingController = bookingController;
            _customerController = customerController;
        }

        /// <summary>
        /// Searches for a customer and loads the customer's bookings.
        /// </summary>
        private void SearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            CustomerSearchMessage.Visibility = Visibility.Collapsed;
            CustomerInformationBorder.Visibility = Visibility.Collapsed;
            NoBookingsMessage.Visibility = Visibility.Collapsed;
            BookingsScrollViewer.Visibility = Visibility.Collapsed;

            string searchItem = CustomerSearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchItem))
            {
                CustomerSearchMessage.Text = "Ange något att söka efter.";
                CustomerSearchMessage.Visibility = Visibility.Visible;
                return;
            }

            Customer customer = _customerController.SearchCustomer(searchItem);

            if (customer == null)
            {
                CustomerSearchMessage.Text = "Ingen kund hittades.";
                CustomerSearchMessage.Visibility = Visibility.Visible;
                return;
            }

            // Displays the correct customer name depending on customer type.
            if (customer is PrivateCustomer privateCustomer)
            {
                CustomerNameTextBlock.Text =
                    $"{privateCustomer.FirstName} {privateCustomer.LastName}";
            }
            else if (customer is CorporateCustomer corporateCustomer)
            {
                CustomerNameTextBlock.Text = corporateCustomer.CompanyName;
            }
            else
            {
                CustomerNameTextBlock.Text = $"Kund #{customer.CustomerID}";
            }

            CustomerEmailTextBlock.Text = customer.Email;
            CustomerInformationBorder.Visibility = Visibility.Visible;

            LoadCustomerBookings(customer.CustomerID);
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
                BookingsScrollViewer.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoBookingsMessage.Visibility = Visibility.Collapsed;
                BookingsScrollViewer.Visibility = Visibility.Visible;
            }
        }

        // Opens the detailed view for the selected booking.
        private void ShowAllInformation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Booking booking)
            {
                Booking specificBooking =
                    _bookingController.GetSpecificBooking(booking.BookingID);

                if (specificBooking != null)
                {
                    BookingDetailsView bookingDetailsView =
                        new BookingDetailsView(specificBooking);

                    bookingDetailsView.ShowDialog();
                }
            }
        }

        // Closes the booking overview and returns to the previous view.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}