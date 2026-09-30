using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Interfaces
{
    /// <summary>
    /// Defines the contract for the Unit of Work pattern.
    /// Orchestrates database transactions and provides a single point of access 
    /// to all repositories to ensure data consistency and atomicity.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for handling customer data.
        /// </summary>
        ICustomerRepository Customer { get; }

        /// <summary>
        /// Repository for managing staff accounts and administrative credentials.
        /// </summary>
        IStaffRepository Staff { get; }

        /// <summary>
        /// Repository for managing bookings made for customers.
        /// </summary>
        IBookingRepository Booking { get; }

        /// <summary>
        /// Repository for managing instances of bookings.
        /// </summary>
        IBookingAccommodationRepository BookingAccommodation { get; }

        /// <summary>
        /// Repository for managing accomodation resources available for booking.
        /// </summary>
        IAccommodationRepository Accommodation { get; }

        /// <summary>
        /// Repository for managing equipment such as skis, boots, scooter rental.
        /// </summary>
        IEquipmentRepository Equipment { get; }

        /// <summary>
        /// Repository for handling skilessons.
        /// </summary>
        ISkiLessonRepository SkiLesson { get; }

        /// <summary>
        /// Repository for managing rentals.
        /// </summary>
        IRentalRepository Rental { get; }

        /// <summary>
        /// Repository for managing invoices and its data.
        /// </summary>
        IInvoiceRepository Invoice { get; }

        /// <summary>
        /// Repository for managing meetingrooms and its properties.
        /// </summary>
        IMeetingRoomRepository MeetingRoom { get; }

        /// <summary>
        /// Repository for managing season prices of accommodation and equipment?.
        /// </summary>
        ISeasonPriceRepository SeasonPrice { get; }

        /// <summary>
        /// Persists all changes tracked by the repositories to the underlying database.
        /// Acts as a commit for the current business transaction.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        int Complete();
    }
}
