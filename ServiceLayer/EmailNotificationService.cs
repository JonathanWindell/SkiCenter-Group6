using EntityLayer;
using System.Net;
using System.Net.Mail;

namespace ServiceLayer
{
    public class EmailNotificationService : INotificationService
    {
        private readonly CustomerEmailTemplate _template;
        private readonly SmtpSettings _settings;

        public EmailNotificationService()
        {
            _template = new CustomerEmailTemplate();
        }

        public void SendEmail(Customer customer, Booking booking)
        {
            if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
            {
                throw new ArgumentException("Customer email address is required.");
            }

            try
            {
                using var mailMessage = new MailMessage();
                // Sender is Ski-Center.
                mailMessage.From = new MailAddress("noreply@skicenter.se", "SkiCenter Bokning");
                // Receiver is customer. 
                mailMessage.To.Add(new MailAddress(customer.Email, $"{customer.DisplayName}".Trim()));

                mailMessage.Subject = $"Bokningsbekräftelse - Bokning #{booking.BookingID}";
                mailMessage.Body = _template.GenerateBookingConfirmation(customer, booking);
                mailMessage.IsBodyHtml = true;

                // Configure SMTP client.
                using var smtpClient = new SmtpClient("smtp.skicenter.se", 587)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true,
                    UseDefaultCredentials = false
                };

                // Send email.
                smtpClient.Send(mailMessage);
            }
            catch (SmtpException smtpEx)
            {
                throw new InvalidOperationException($"Failed to send email via SMTP: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An unexpected error occurred while sending email: {ex.Message}", ex);
            }
        }
    }
}