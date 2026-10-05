using EntityLayer;

namespace ServiceLayer
{
    public interface INotificationService
    {
        void SendEmail(Customer customer, Booking booking);
    }
}
