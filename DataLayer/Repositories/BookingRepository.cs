using DataLayer.Interfaces;
using EntityLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        private readonly SkiCenterDbContext _context;

        public BookingRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
