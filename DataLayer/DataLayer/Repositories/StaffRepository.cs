using System;
using System.Collections.Generic;
using System.Text;
using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        private readonly SkiCenterDbContext _context;

        public StaffRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
