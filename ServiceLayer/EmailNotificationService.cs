using BusinessLayer;
using EntityLayer;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ServiceLayer
{
    public class EmailNotificationService
    {
        private readonly CustomerEmailTemplate _template;
        private readonly IConfiguration _config;

        public EmailNotificationService(IConfiguration config)
        {
            // Template is found in CustomerEmailTemplate (HTML structure).
            _template = new CustomerEmailTemplate();
            _config = config;
        }

        /// <summary>
        /// Method for sending email to customer containing everything tied to specific booking. 
        /// </summary>
        public void SendEmail(Customer customer, Booking booking, List<PaymentInstallment> paymentPlan)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new ArgumentException("Kunden saknar e-postadress.");

            try
            {
                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("noreply@skicenter.se", "SkiCenter Bokning");
                mailMessage.To.Add(new MailAddress(customer.Email, customer.DisplayName.Trim()));

                mailMessage.Subject = $"Bokningsbekräftelse - Bokning #{booking.BookingID}";

                // Generate entire confirmation as HTML
                string htmlContent = _template.GenerateBookingConfirmation(customer, booking, paymentPlan);

                // Fill mail with created HTML
                mailMessage.Body = htmlContent;
                mailMessage.IsBodyHtml = true;

                // Fetch SMTP settings from appsettings.json
                // Variables accepted are defined in SmtpSettings.cs
                using var smtpClient = new SmtpClient(_config["SmtpSettings:Host"], int.Parse(_config["SmtpSettings:Port"]))
                {
                    Credentials = new NetworkCredential(_config["SmtpSettings:Username"], _config["SmtpSettings:Password"]),
                    EnableSsl = true
                };

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ett fel uppstod när mailet skulle skickas: {ex.Message}", ex);
            }
        }
    }
}


