using DataLayer.Interfaces;
using DataLayer.Repositories;

namespace DataLayer
{
    /// <summary>
    /// Handles transactions and coordinates repositorys and uses SkiCenterDbContext for database connection. 
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        // Instansiate readonly of database connection
        private readonly SkiCenterDbContext _context;

        public ICustomerRepository Customer { get; private set; }
        public IStaffRepository Staff { get; private set; }
        public IBookingRepository Booking { get; private set; }
        public IBookingAccommodationRepository BookingAccommodation { get; private set; }
        public IAccommodationRepository Accommodation { get; private set; }
        public IEquipmentRepository Equipment { get; private set; }
        public IEquipmentItemRepository EquipmentItem { get; private set; }
        public ISkiLessonRepository SkiLesson { get; private set; }
        public IRentalRepository Rental { get; private set; }
        public IInvoiceRepository Invoice { get; private set; }
        public IMeetingRoomRepository MeetingRoom { get; private set; }
        public ISeasonPriceRepository SeasonPrice { get; private set; }

        public UnitOfWork(SkiCenterDbContext context)
        {
            _context = context;

            Customer = new CustomerRepository(_context);
            Staff = new StaffRepository(_context);
            Booking = new BookingRepository(_context);
            BookingAccommodation = new BookingAccommodationRepository(_context);
            Accommodation = new AccommodationRepository(_context);
            Equipment = new EquipmentRepository(_context);
            EquipmentItem = new EquipmentItemRepository(_context);
            SkiLesson = new SkiLessonRepository(_context);
            Rental = new RentalRepository(_context);
            Invoice = new InvoiceRepository(_context);
            MeetingRoom = new MeetingRoomRepository(_context);
            SeasonPrice = new SeasonPriceRepository(_context);
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
