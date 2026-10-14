using System.Globalization;
using System.Windows;
using System.Windows.Data;
using EntityLayer;

namespace PresentationLayer.Views
{
    public partial class BookingDetailsView : Window
    {
        public BookingDetailsView(Booking booking)
        {
            InitializeComponent();
            DataContext = booking;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Converts booking statuses to Swedish display text.
    public class BookingStatusSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Preliminary" => "Preliminär",
                "Confirmed" => "Bekräftad",
                "Cancelled" => "Avbokad",
                "Canceled" => "Avbokad",
                "Paid" => "Betald",
                _ => value?.ToString() ?? ""
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // Converts accommodation statuses to Swedish display text.
    public class AccommodationStatusSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Available" => "Tillgänglig",
                "Rented" => "Uthyrd",
                "Damaged" => "Skadad",
                "Maintenance" => "Under underhåll",
                _ => value?.ToString() ?? ""
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // Converts boolean values to Swedish Yes or No.
    public class BooleanSwedishConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is bool booleanValue
                ? (booleanValue ? "Ja" : "Nej")
                : "";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
