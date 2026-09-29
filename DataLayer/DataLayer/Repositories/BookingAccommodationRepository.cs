using DataLayer.Interfaces;
using EntityLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Repositories
{
    public class BookingAccommodationRepository : Repository<BookingAccommodation>, IBookingAccommodationRepository
    {
        private readonly SkiCenterDbContext _context;

        public BookingAccommodationRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
