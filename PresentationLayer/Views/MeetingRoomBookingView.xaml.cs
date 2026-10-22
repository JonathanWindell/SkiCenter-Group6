using BusinessLayer;
using EntityLayer;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace PresentationLayer.Views
{
    public partial class MeetingRoomBookingView : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private Booking _booking;

        public MeetingRoomBookingView(IServiceProvider serviceProvider, Booking booking)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _booking = booking;

            // Get booking start- and endDate
            DateTime minDate = _booking.Accommodations?.Min(a => a.StartDate) ?? _booking.BookingDate;
            DateTime maxDate = _booking.Accommodations?.Max(a => a.EndDate) ?? _booking.BookingDate.AddDays(1);

            StartDatePicker.DisplayDateStart = minDate;
            StartDatePicker.DisplayDateEnd = maxDate;
            EndDatePicker.DisplayDateStart = minDate;
            EndDatePicker.DisplayDateEnd = maxDate;

            StartDatePicker.SelectedDate = minDate;
            EndDatePicker.SelectedDate = minDate.AddDays(1) <= maxDate ? minDate.AddDays(1) : maxDate;

            // Load roms that are coupled to booking.
            LoadBookedRooms();
        }

        /// <summary>
        /// Gets all meeting rooms coupled to booking. 
        /// </summary>
        private void LoadBookedRooms()
        {
            var controller = _serviceProvider.GetRequiredService<BookingController>();
            _booking = controller.GetSpecificBooking(_booking.BookingID);

            if (_booking?.MeetingRooms != null)
            {
                BookedMeetingRoomsDataGrid.ItemsSource = _booking.MeetingRooms.ToList();
            }
        }

        /// <summary>
        /// Gets all meeting rooms that have status = "Available" between start- and endDate
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null) return;

            try
            {
                var unitOfWork = _serviceProvider.GetRequiredService<DataLayer.Interfaces.IUnitOfWork>();
                var availableRooms = unitOfWork.MeetingRoom.GetAvailableMeetingRooms(
                    StartDatePicker.SelectedDate.Value,
                    EndDatePicker.SelectedDate.Value);

                MeetingRoomsDataGrid.ItemsSource = availableRooms.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte söka efter mötesrum: {ex.Message}");
            }
        }

        /// <summary>
        /// Button to add meeting room and book it.
        /// </summary>
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (MeetingRoomsDataGrid.SelectedItem is not MeetingRoom selectedRoom)
            {
                MessageBox.Show("Välj ett ledigt rum i nedre listan först.");
                return;
            }

            try
            {
                var controller = _serviceProvider.GetRequiredService<BookingController>();
                controller.AddMeetingRoomToBooking(
                    _booking.BookingID,
                    selectedRoom.MeetingRoomID,
                    StartDatePicker.SelectedDate.Value,
                    EndDatePicker.SelectedDate.Value);

                LoadBookedRooms(); // Update top list
                MessageBox.Show("Mötesrummet har lagts till i bokningen!", "Klart", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte lägga till mötesrummet: {ex.Message}");
            }
        }

        /// <summary>
        /// Button to remove booked meeting room.
        /// </summary>
        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (BookedMeetingRoomsDataGrid.SelectedItem is not BookingMeetingRoom selectedConnection)
            {
                MessageBox.Show("Välj ett rum i den övre listan att ta bort.");
                return;
            }

            var result = MessageBox.Show("Är du säker på att du vill ta bort mötesrummet?", "Bekräfta", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var controller = _serviceProvider.GetRequiredService<BookingController>();
                    controller.RemoveMeetingRoomFromBooking(_booking.BookingID, selectedConnection.BookingMeetingRoomID);

                    LoadBookedRooms(); // Update top list
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kunde inte ta bort mötesrummet: {ex.Message}");
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}