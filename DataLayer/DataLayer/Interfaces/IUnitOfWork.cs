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
        /// Repository for managing accomodation resources available for booking.
        /// </summary>
        IAccomodationRepository Accomodation { get; }

        /// <summary>
        /// Repository for managing equipment such as skis, boots, scooter rental.
        /// </summary>
        IEquipmentRepository Equipment { get; }

        /// <summary>
        /// Repository for .
        /// </summary>
        ISkiLessionSessionRepository SkiLessionSession { get; }

        /// <summary>
        /// Repository for managing inventory and hardware associated with resources.
        /// </summary>
        IInvoiceRepository Invoice { get; }

        /// <summary>
        /// Persists all changes tracked by the repositories to the underlying database.
        /// Acts as a commit for the current business transaction.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        int Complete();
    }
}
