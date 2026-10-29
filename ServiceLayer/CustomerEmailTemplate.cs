using BusinessLayer;
using EntityLayer;
using System.Text;

namespace ServiceLayer
{
    public class CustomerEmailTemplate
    {
        public string GenerateBookingConfirmation(Customer customer, Booking booking, List<PaymentInstallment> paymentPlan)
        {
            var sb = new StringBuilder();

            // Calculate start- and enddate (Uses booking date as fallback). 
            DateTime checkIn = booking.Accommodations != null && booking.Accommodations.Any()
                ? booking.Accommodations.Min(a => a.StartDate)
                : booking.BookingDate;

            DateTime checkOut = booking.Accommodations != null && booking.Accommodations.Any()
                ? booking.Accommodations.Max(a => a.EndDate)
                : booking.BookingDate;

            sb.Append("<html><body style='font-family: Arial, sans-serif; color: #333;'>");
            sb.Append($"<h2>Bokningsbekräftelse & Fakturaunderlag</h2>");
            sb.Append($"<p>Hej {customer.DisplayName}!</p>");
            sb.Append($"<p>Tack för din bokning. Här kommer dina bokningsdetaljer och betalningsinformation.</p>");

            sb.Append("<hr/>");
            sb.Append($"<p><strong>Bokningsnummer:</strong> #{booking.BookingID}</p>");
            sb.Append($"<p><strong>Bokningsdatum:</strong> {booking.BookingDate:yyyy-MM-dd}</p>");
            sb.Append($"<p><strong>Ankomst:</strong> {checkIn:yyyy-MM-dd} | <strong>Avresa:</strong> {checkOut:yyyy-MM-dd}</p>");

            // Create - Accommodation
            if (booking.Accommodations != null && booking.Accommodations.Count > 0)
            {
                sb.Append("<h3>Bokat boende</h3><ul>");
                foreach (var ba in booking.Accommodations)
                {
                    sb.Append($"<li>Lägenhet/Stuga: {ba.Accommodation?.AccommodationNumber ?? ba.AccommodationID.ToString()}</li>");
                }
                sb.Append("</ul>");
            }

            // Create - Equipment
            if (booking.Rentals != null && booking.Rentals.Any())
            {
                sb.AppendLine("<h3>Hyrd utrustning</h3>");
                sb.AppendLine("<ul>");
                foreach (var rental in booking.Rentals)
                {
                    // Write total sum for entire rental stay.
                    sb.AppendLine($"<li><strong>Hyresperiod ({rental.StartDate:yyyy-MM-dd} till {rental.EndDate:yyyy-MM-dd}) - Pris: {rental.TotalPrice:N2} kr</strong><ul>");

                    // List what equipment is rented (without price)
                    foreach (var rentalItem in rental.RentalItems)
                    {
                        string itemName = rentalItem.EquipmentItem?.Equipment?.DisplayName ?? "Utrustning";
                        string size = string.IsNullOrWhiteSpace(rentalItem.EquipmentItem?.Size) ? "Standard" : rentalItem.EquipmentItem.Size;

                        sb.AppendLine($"<li>{itemName} (Stl: {size})</li>");
                    }
                }
                sb.AppendLine("</ul>");
            }

            // Create - Ski School 
            if (booking.SkiLessons != null && booking.SkiLessons.Any())
            {
                sb.AppendLine("<h3>Skidskola & Lektioner</h3>");
                sb.AppendLine("<ul>");
                foreach (var lesson in booking.SkiLessons)
                {
                    string level = lesson.SkiLessonSession?.SkiLesson?.Level ?? "Okänd nivå";
                    int week = lesson.SkiLessonSession?.WeekNumber ?? 0;
                    string days = lesson.SkiLessonSession?.SkiLesson?.Days ?? "Okända dagar";
                    string time = lesson.SkiLessonSession?.TimeSlot ?? "Okänd tid";
                    decimal price = lesson.SkiLessonSession?.SkiLesson?.Price ?? 0;

                    sb.AppendLine($"<li>{level} (V.{week}, {days}, {time}) - Pris: {price:N2} kr</li>");
                }
                sb.AppendLine("</ul>");
            }

            // Create - Meeting Rooms
            if (booking.MeetingRooms != null && booking.MeetingRooms.Any())
            {
                sb.AppendLine("<h3>Bokat mötesrum</h3>");
                sb.AppendLine("<ul>");
                foreach (var mr in booking.MeetingRooms)
                {
                    string roomNumber = mr.meetingRoom?.MeetingRoomNumber ?? "Okänt rum";

                    sb.AppendLine($"<li>{roomNumber} ({mr.StartDate:yyyy-MM-dd} till {mr.EndDate:yyyy-MM-dd}) - Pris: {mr.TotalPrice:N2} kr</li>");
                }
                sb.AppendLine("</ul>");
            }

            // Add Cancellation Insurance if customer has choosen. 
            if (booking.HasCancellationInsurance)
            {
                sb.Append("<p><strong>Inkluderar Avbeställningsskydd (300,00 kr)</strong></p>");
            }

            // Financial calculation. Summarises to 12% currently. 
            decimal totalInclVat = booking.TotalAmount;
            decimal vatRate = 0.12m;
            decimal totalExclVat = totalInclVat / (1 + vatRate);
            decimal vatAmount = totalInclVat - totalExclVat;

            sb.Append("<hr/>");
            sb.Append($"<p>Totalt belopp exkl. moms: {totalExclVat:C}</p>");
            sb.Append($"<p>Varav moms (12%): {vatAmount:C}</p>");
            sb.Append($"<h3><strong>Att betala (inkl. moms): {totalInclVat:C}</strong></h3>");

            // Paymentplan - (PaymentPlanService.cs)
            if (paymentPlan != null && paymentPlan.Any())
            {
                sb.Append("<hr/>");
                sb.Append("<h3>Betalningsplan</h3>");
                sb.Append("<ul>");
                foreach (var installment in paymentPlan)
                {
                    sb.Append($"<li><strong>{installment.DueDate:yyyy-MM-dd}:</strong> {installment.Amount:C} <em>({installment.Description})</em></li>");
                }
                sb.Append("</ul>");
            }

            sb.Append("<p><br/>Varmt välkomna till oss!<br/><em>SkiCenter Teamet</em></p>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}