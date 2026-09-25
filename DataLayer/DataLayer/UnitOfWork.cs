using DataLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer
{
    /// <summary>
    /// Handles transactions and coordinates repositorys and uses SkiCenterDbContext for database connection. 
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        // Instansiate readonly of database connection
        private readonly SkiCenterDbContext _context;


        // 
        public ICustomerRepository Customer { get; private set; }
        public IStaffRepository Staff { get; private set; }
        public IBookingRepository Booking { get; private set; }
        public IAccomodationRepository Accomodation { get; private set; }
        public IEquipmentRepository Equipment { get; private set; }
        public ISkiLessionSessionRepository SkiLessionSession { get; private set; }
        public IInvoiceRepository SkiLessionSession { get; private set; }


        public UnitOfWork(SkiCenterDbContext context)
        {
            _context = context;

            Customer = new CustomerRepository(_context);
            Staff = new StaffRepository(_context);
            Booking = new BookingRepository(_context);
            Accomodation = new AccomodationRepository(_context);
            Equipment = new EquipmentRepository(_context);
            SkiLessionSession = new SkiLessionSessionRepository(_context);
            IInvoice = new EquipmentRepository(_context);
        }


        /// <summary>
        /// Writes to database when queries are done. 
        /// </summary>
        public int Complete()
        {
            return _context.SaveChanges();
        }


        /// <summary>
        /// Release resources and close database connection. 
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
