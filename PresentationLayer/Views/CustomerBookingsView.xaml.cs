
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessLayer;
using BusinessLayer.Controllers;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for CustomerBookingsView.xaml.
    /// </summary>
    public partial class CustomerBookingsView : Window
    {
        private readonly BookingController _bookingController;
        private readonly CustomerController _customerController;
        private readonly IServiceProvider _serviceProvider;

        private int? _selectedCustomerId;

        public CustomerBookingsView(
            BookingController bookingController,
            CustomerController customerController,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _bookingController = bookingController;
            _customerController = customerController;
            _serviceProvider = serviceProvider;
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

            _selectedCustomerId = null;
            BookingsItemsControl.ItemsSource = null;

            string searchItem = CustomerSearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchItem))
            {
                CustomerSearchMessage.Text = "Ange något att söka efter.";
                CustomerSearchMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                Customer? customer =
                    _customerController.SearchCustomer(searchItem);

                if (customer == null)
                {
                    CustomerSearchMessage.Text = "Ingen kund hittades.";
                    CustomerSearchMessage.Visibility = Visibility.Visible;
                    return;
                }

                // Display the correct name based on customer type.
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
                    CustomerNameTextBlock.Text =
                        $"Kund #{customer.CustomerID}";
                }

                CustomerEmailTextBlock.Text = customer.Email;
                CustomerInformationBorder.Visibility = Visibility.Visible;

                _selectedCustomerId = customer.CustomerID;

                LoadCustomerBookings(customer.CustomerID);
            }
            catch (Exception ex)
            {
                CustomerSearchMessage.Text =
                    $"Kunde inte söka efter kunden: {ex.Message}";

                CustomerSearchMessage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Loads all bookings belonging to the selected customer.
        /// </summary>
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

        /// <summary>
        /// Opens the form for creating a new booking.
        /// </summary>
        private void CreateNewBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Resolve the booking window using dependency injection.
                CreateBookingView createBookingView =
                    _serviceProvider.GetRequiredService<CreateBookingView>();

                createBookingView.Owner = this;

                // Wait until the booking window is closed.
                bool? result = createBookingView.ShowDialog();

                // Refresh the booking list after a successful booking.
                if (result == true && _selectedCustomerId.HasValue)
                {
                    LoadCustomerBookings(_selectedCustomerId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Kunde inte öppna eller uppdatera bokningen.\n{ex.Message}",
                    "Fel vid bokning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the detailed view for the selected booking.
        /// </summary>
        private void ShowAllInformation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.DataContext is Booking booking)
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

        /// <summary>
        /// Closes the booking overview.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
