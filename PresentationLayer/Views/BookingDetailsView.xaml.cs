using System.Windows;
using EntityLayer;

namespace PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for BookingDetailsView.xaml
    /// </summary>
    public partial class BookingDetailsView : Window
    {
        public BookingDetailsView(Booking booking)
        {
            InitializeComponent();

            DataContext = booking;
        }

        // Closes the detailed view and returns to the booking overview.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}