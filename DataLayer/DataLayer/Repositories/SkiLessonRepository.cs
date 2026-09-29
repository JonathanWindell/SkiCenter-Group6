using System;
using System.Collections.Generic;
using System.Text;
using DataLayer.Interfaces;
using EntityLayer;

namespace DataLayer.Repositories
{
    public class SkiLessonRepository : Repository<SkiLesson>, ISkiLessonRepository
    {
        private readonly SkiCenterDbContext _context;

        public SkiLessonRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
