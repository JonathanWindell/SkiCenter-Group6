using DataLayer;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class PropertyController
    {
        private readonly UnitOfWork _unitOfWork;

        public PropertyController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Accommodation> GetAllAccommodations()
        {
            return _unitOfWork.Accommodation.GetAllAccommodations();
        }

        public IEnumerable<MeetingRoom> GetAllMeetingRooms()
        {
            return _unitOfWork.MeetingRoom.GetAllMeetingRooms();
        }

        public bool UpdateAccommodationStatus(int accommodationId, AccommodationStatus newStatus)
        {
            var accommodation = _unitOfWork.Accommodation.GetByID(accommodationId);
            if (accommodation == null) return false;

            accommodation.Status = newStatus;
            _unitOfWork.Complete();
            return true;
        }

        public bool UpdateMeetingRoomStatus(int meetingRoomId, MeetingRoomStatus newStatus)
        {
            var meetingRoom = _unitOfWork.MeetingRoom.GetByID(meetingRoomId);
            if (meetingRoom == null) return false;

            meetingRoom.Status = newStatus;
            _unitOfWork.Complete();
            return true;
        }
    }
}