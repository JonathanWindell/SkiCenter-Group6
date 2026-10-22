using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface IMeetingRoomRepository : IRepository<MeetingRoom>
    {
        // Defines function for fetching meeting rooms by status
        IEnumerable<MeetingRoom> GetMeetingRoomsByStatus(MeetingRoomStatus status);

        // Defines function for fetching meeting rooms by capacity
        IEnumerable<MeetingRoom> GetMeetingRoomsByCapacity(int minCapacity);

        // Defines function for fetching all meeting rooms 
        IEnumerable<MeetingRoom> GetAllMeetingRooms();

        // Defines function for fetching meeting rooms dates
        IEnumerable<MeetingRoom> GetAvailableMeetingRooms(DateTime startDate, DateTime endDate);
    }
}
