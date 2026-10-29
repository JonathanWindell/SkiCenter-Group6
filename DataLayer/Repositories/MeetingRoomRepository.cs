using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class MeetingRoomRepository : Repository<MeetingRoom>, IMeetingRoomRepository
    {
        #region Constructor & Fields

        private readonly SkiCenterDbContext _context;

        public MeetingRoomRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        #endregion

        #region Availability Queries

        /// <summary>
        /// Gets available meeting rooms for a specific date range.
        /// </summary>
        public IEnumerable<MeetingRoom> GetAvailableMeetingRooms(DateTime startDate, DateTime endDate)
        {
            // Fetch all available meeting rooms where there are no overlapping bookings.
            return _context.Set<MeetingRoom>()
                .Where(m => !_context.Set<BookingMeetingRoom>().Any(bmr =>
                    bmr.MeetingRoomID == m.MeetingRoomID &&
                    bmr.StartDate < endDate &&
                    bmr.EndDate > startDate))
                .AsNoTracking()
                .ToList();
        }

        #endregion

        #region Filtered & List Queries

        /// <summary>
        /// Gets all MeetingRooms based on status.
        /// </summary>
        /// <param name="status"></param>
        public IEnumerable<MeetingRoom> GetMeetingRoomsByStatus(MeetingRoomStatus status)
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

        /// <summary>
        /// Gets all meeting rooms registered in the system.
        /// </summary>
        public IEnumerable<MeetingRoom> GetAllMeetingRooms()
        {
            return _context.Set<MeetingRoom>()
                .AsNoTracking()
                .ToList();
        }

        #endregion
    }
}