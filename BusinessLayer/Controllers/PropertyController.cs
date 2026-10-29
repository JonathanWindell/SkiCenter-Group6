using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class PropertyController
    {
        #region Fields and constructor

        private readonly IUnitOfWork _unitOfWork;

        public PropertyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Accommodation and meeting room queries

        public IEnumerable<Accommodation> GetAllAccommodations()
        {
            return _unitOfWork.Accommodation.GetAllAccommodations();
        }

        public IEnumerable<MeetingRoom> GetAllMeetingRooms()
        {
            return _unitOfWork.MeetingRoom.GetAllMeetingRooms();
        }

        #endregion

        #region Status updates

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

        #endregion
    }
}