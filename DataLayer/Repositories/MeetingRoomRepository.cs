using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class MeetingRoomRepository : Repository<MeetingRoom>, IMeetingRoomRepository
    {
        private readonly SkiCenterDbContext _context;

        public MeetingRoomRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

       
    }
}
