using System;
using System.Collections.Generic;
using System.Text;
using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class SeasonPriceRepository : Repository<SeasonPrice>, ISeasonPriceRepository
    {
        private readonly SkiCenterDbContext _context;

        public SeasonPriceRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
