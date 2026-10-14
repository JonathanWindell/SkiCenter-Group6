using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class SkiSchoolController
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkiSchoolController"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance for database transactions.</param>
        public SkiSchoolController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Gets all ski lesson course types available in the system.
        /// </summary>
        public IEnumerable<SkiLesson> GetAllLessons()
        {
            return _unitOfWork.SkiLesson.GetAll();
        }

        /// <summary>
        /// Gets all ski lessons matching a specific difficulty level.
        /// </summary>
        /// <param name="level">The skill level (e.g., Grön, Blå, Röd, Svart, Privatlektion).</param>
        public IEnumerable<SkiLesson> GetLessonsByLevel(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                return Enumerable.Empty<SkiLesson>();
            }

            return _unitOfWork.SkiLesson.GetLessonsByLevel(level);
        }

        /// <summary>
        /// Gets all available sessions for a given week and level where the group is not fully booked.
        /// </summary>
        /// <param name="weekNumber">The calendar week number.</param>
        /// <param name="level">The skill level.</param>
        public IEnumerable<SkiLessonSession> GetAvailableSessions(int weekNumber, string level)
        {
            if (weekNumber < 1 || weekNumber > 53 || string.IsNullOrWhiteSpace(level))
            {
                return Enumerable.Empty<SkiLessonSession>();
            }

            return _unitOfWork.SkiLesson.GetAvailableSessions(weekNumber, level);
        }

        /// <summary>
        /// Books a participant to a ski lesson session under an existing booking order.
        /// </summary>
        /// <param name="bookingId">The identifier of the customer booking.</param>
        /// <param name="sessionId">The identifier of the lesson session.</param>
        public bool BookParticipant(int bookingId, int sessionId)
        {
            if (bookingId <= 0 || sessionId <= 0)
            {
                return false;
            }

            // Verify that the parent booking exists
            var booking = _unitOfWork.Booking.GetByID(bookingId);
            if (booking == null)
            {
                return false;
            }

            // Fetch session with lesson details to inspect participant limits
            var lesson = _unitOfWork.SkiLesson.GetAll()
                .FirstOrDefault(l => l.Sessions.Any(s => s.SkiLessonSessionID == sessionId));

            var session = lesson?.Sessions.FirstOrDefault(s => s.SkiLessonSessionID == sessionId);
            if (session == null)
            {
                return false;
            }

            // Validate that the session is not already full
            int currentParticipants = _unitOfWork.SkiLesson.GetBookedParticipantCount(sessionId);
            if (currentParticipants >= lesson.MaxParticipants)
            {
                return false;
            }

            // Create and persist the lesson booking relation
            var lessonBooking = new SkiLessonBooking
            {
                BookingID = bookingId,
                SkiLessonSessionID = sessionId
            };

            // Add lesson price to the booking total amount
            booking.TotalAmount += lesson.Price;
            _unitOfWork.Booking.Update(booking);

            // If a specific repository exists for SkiLessonBooking, add through it; otherwise via session
            session.Participants.Add(lessonBooking);
            _unitOfWork.SkiLesson.Update(lesson);

            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Cancels a booked ski lesson participant and adjusts the booking total amount.
        /// </summary>
        /// <param name="skiLessonBookingId">The unique identifier of the ski lesson booking.</param>
        public bool CancelParticipant(int skiLessonBookingId)
        {
            if (skiLessonBookingId <= 0)
            {
                return false;
            }

            // Locate lesson booking through the entity hierarchy
            var lesson = _unitOfWork.SkiLesson.GetAll()
                .FirstOrDefault(l => l.Sessions.Any(s => s.Participants.Any(p => p.SkiLessonBookingID == skiLessonBookingId)));

            var session = lesson?.Sessions.FirstOrDefault(s => s.Participants.Any(p => p.SkiLessonBookingID == skiLessonBookingId));
            var participantBooking = session?.Participants.FirstOrDefault(p => p.SkiLessonBookingID == skiLessonBookingId);

            if (participantBooking == null)
            {
                return false;
            }

            var booking = _unitOfWork.Booking.GetByID(participantBooking.BookingID);
            if (booking != null)
            {
                booking.TotalAmount = Math.Max(0, booking.TotalAmount - lesson.Price);
                _unitOfWork.Booking.Update(booking);
            }

            session.Participants.Remove(participantBooking);
            _unitOfWork.SkiLesson.Update(lesson);

            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Checks whether a specific session has available spots for registration.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public bool IsSessionAvailable(int sessionId)
        {
            if (sessionId <= 0)
            {
                return false;
            }

            var lesson = _unitOfWork.SkiLesson.GetAll()
                .FirstOrDefault(l => l.Sessions.Any(s => s.SkiLessonSessionID == sessionId));

            if (lesson == null)
            {
                return false;
            }

            int count = _unitOfWork.SkiLesson.GetBookedParticipantCount(sessionId);
            return count < lesson.MaxParticipants;
        }
    }
}