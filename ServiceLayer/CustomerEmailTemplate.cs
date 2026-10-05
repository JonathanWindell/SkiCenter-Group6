using EntityLayer;
using System.Text;

namespace ServiceLayer
{
    public class CustomerEmailTemplate
    {
        public string GenerateBookingConfirmation(Customer customer, Booking booking)
        {
            var sb = new StringBuilder();

            sb.Append("<html><body style='font-family: Arial, sans-serif;'>");
            sb.Append($"<h2>Bokningsbekräftelse - SkiCenter</h2>");
            sb.Append($"<p>Hej {customer.DisplayName}!</p>");
            sb.Append($"<p>Tack för din bokning. Här kommer dina bokningsdetaljer:</p>");

            sb.Append("<hr/>");
            sb.Append($"<p><strong>Bokningsnummer:</strong> #{booking.BookingID}</p>");
            sb.Append($"<p><strong>Bokningen genomfördes:</strong> {booking.BookingDate:yyyy-MM-dd}</p>");

            // If booking has rentals tied to it. 
            if (booking.Rentals != null && booking.Rentals.Count > 0)
            {
                sb.Append("<h3>Hyrd utrustning</h3><ul>");
                foreach (var rental in booking.Rentals)
                {
                    int days = (rental.EndDate.Date - rental.StartDate.Date).Days;
                    if (days <= 0) days = 1;

                    foreach (var item in rental.EquipmentItems)
                    {
                        string itemName = item.Equipment?.Description ?? "Utrustning";

                        var priceMatrix = item.Equipment?.Prices?.FirstOrDefault(p => p.Days == days);
                        decimal itemPrice = priceMatrix != null ? priceMatrix.TotalAmount : 0;

                        sb.Append($"<li>{itemName} (Stl: {item.Size}) - Pris för {days} dagar: {itemPrice:C}</li>");
                    }
                }
                sb.Append("</ul>");
            }

            // If booking has accomodation tied to it.
            if (booking.Accommodations != null && booking.Accommodations.Count > 0)
            {
                sb.Append("<h3>Bokat boende</h3><ul>");
                foreach (var ba in booking.Accommodations)
                {
                    sb.Append($"<li>Lägenhet/Stuga ID: {ba.AccommodationID}</li>");
                }
                sb.Append("</ul>");
            }

            sb.Append("<hr/>");
            sb.Append($"<p><strong>Totalsumma:</strong> {booking.TotalAmount:C}</p>");
            sb.Append("<p>Varmt välkomna till oss!<br/><em>SkiCenter Teamet</em></p>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}
