using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IMeetingRoomRepository : IRepository<MeetingRoom>
    {
        public interface IMeetingRoomRepository : IRepository<MeetingRoom>
        {
            IEnumerable<MeetingRoom> GetMeetingRoomsByStatus(meetingRoomStatus status);
            IEnumerable<MeetingRoom> GetMeetingRoomsByCapacity(int minCapacity);
            IEnumerable<MeetingRoom> GetAvailableMeetingRooms(DateTime startDate, DateTime endDate, int minCapacity = 0);
            bool IsMeetingRoomAvailable(int meetingRoomId, DateTime startDate, DateTime endDate);
        }
    }
}
