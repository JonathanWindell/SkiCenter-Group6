using System;
using System.Collections.Generic;
using System.Text;
using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        private readonly SkiCenterDbContext _context;

        public CustomerRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
