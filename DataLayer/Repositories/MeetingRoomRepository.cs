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

        /// <summary>
        /// Gets all MeetingRooms based on status.
        /// </summary>
        /// <param name="status"></param>
        public IEnumerable<MeetingRoom> GetMeetingRoomsByStatus(meetingRoomStatus status)
        {
            return _context.Set<MeetingRoom>()
                .Where(m => m.Status == status)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Gets all MeetingRooms based on minimum capacity.
        /// </summary>
        /// <param name="minCapacity"></param>
        public IEnumerable<MeetingRoom> GetMeetingRoomsByCapacity(int minCapacity)
        {
            return _context.Set<MeetingRoom>()
                .Where(m => m.Capacity >= minCapacity)
                .AsNoTracking()
                .ToList();
        }

        // MISSING 2 FUNCTIONS, WIP
    }
}
