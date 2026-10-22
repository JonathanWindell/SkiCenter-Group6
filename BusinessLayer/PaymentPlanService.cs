using EntityLayer;

namespace BusinessLayer
{
    public class PaymentInstallment
    {
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Description { get; set; }
    }

    public class PaymentPlanService
    {
        public List<PaymentInstallment> GeneratePaymentPlan(Booking booking)
        {
            var plan = new List<PaymentInstallment>();

            // Find the actual arrival date by searching for earliest found date in booking. Always tied to Accommodation in this version. 
            // If no dates are found, fallback to booking date.
            DateTime arrivalDate = booking.BookingDate.Date;

            var allStartDates = new List<DateTime>();

            if (booking.Accommodations != null && booking.Accommodations.Any())
                allStartDates.Add(booking.Accommodations.Min(a => a.StartDate).Date);

            if (booking.Rentals != null && booking.Rentals.Any())
                allStartDates.Add(booking.Rentals.Min(r => r.StartDate).Date);

            if (allStartDates.Any())
            {
                arrivalDate = allStartDates.Min();
            }

            // Calculate number of days until date of booking and arrival date.
            int daysUntilArrival = (arrivalDate - booking.BookingDate.Date).Days;

            // Amount for insurance
            decimal insuranceCost = booking.HasCancellationInsurance ? 300m : 0m;

            // Baseamount (Exlusive insurance) 
            decimal baseAmount = booking.TotalAmount - insuranceCost;

            if (daysUntilArrival < 30)
            {
                // If booking is less than 30 days away. Payment plan should be one payment.
                plan.Add(new PaymentInstallment
                {
                    InstallmentNumber = 1,
                    Amount = booking.TotalAmount,
                    DueDate = booking.BookingDate.Date,
                    Description = "Slutbetalning (Förfaller omedelbart)"
                });
            }
            else
            {
                // If booking is more than 30 days away. Payment plan should be split into multiple payments.
                decimal deposit = (baseAmount * 0.20m) + insuranceCost;
                decimal remainder = baseAmount * 0.80m;

                plan.Add(new PaymentInstallment
                {
                    InstallmentNumber = 1,
                    Amount = deposit,
                    DueDate = booking.BookingDate.Date.AddDays(30),
                    Description = "Anmälningsavgift (20%) inkl. ev. avbeställningsskydd"
                });

                plan.Add(new PaymentInstallment
                {
                    InstallmentNumber = 2,
                    Amount = remainder,
                    DueDate = arrivalDate.AddDays(-30),
                    Description = "Slutbetalning"
                });
            }

            return plan;
        }
    }
}