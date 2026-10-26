using DataLayer.Interfaces;
using EntityLayer;
using System.Globalization;
using System.Text;

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
        /// Gets all ski lessons including their scheduled sessions and booked participants.
        /// </summary>
        public IEnumerable<SkiLesson> GetAllLessonsWithSessions()
        {
            return _unitOfWork.SkiLesson.GetLessonsWithSessions();
        }

        /// <summary>
        /// Gets all active and confirmed bookings that can have ski lessons added.
        /// </summary>
        public IEnumerable<Booking> GetActiveBookings()
        {
            return _unitOfWork.Booking.GetAllBookings()
                .Where(b => b.BookingStatus != BookingStatus.Cancelled && b.BookingStatus != BookingStatus.Finished)
                .OrderByDescending(b => b.BookingID)
                .ToList();
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
        public (bool success, string message) BookParticipant(int bookingId, int sessionId)
        {
            if (bookingId <= 0 || sessionId <= 0)
            {
                return (false, "Ogiltigt boknings- eller pass-ID.");
            }

            // Verify that the parent booking exists
            var booking = _unitOfWork.Booking.GetByID(bookingId);
            if (booking == null)
            {
                return (false, "Kunde inte hitta bokningen.");
            }

            // Fetch session with lesson details to inspect participant limits
            var lesson = _unitOfWork.SkiLesson.GetAll()
                .FirstOrDefault(l => l.Sessions.Any(s => s.SkiLessonSessionID == sessionId));

            var session = lesson?.Sessions.FirstOrDefault(s => s.SkiLessonSessionID == sessionId);
            if (session == null)
            {
                return (false, "Kunde inte hitta skidskolepasset.");
            }

            // Verify that Ski school week mathces customers booked week. 
            bool isCorrectWeek = false;

            var accommodationBooking = booking.Accommodations?.FirstOrDefault();

            if (accommodationBooking == null)
            {
                return (false, "Bokningen saknar ett kopplat boende. Det går inte att verifiera vilka veckor kunden är på plats.");
            }

            // Fetch date from Booking (Accommodation start- and enddate)
            DateTime stayStart = accommodationBooking.StartDate.Date;
            DateTime stayEnd = accommodationBooking.EndDate.Date;

            for (DateTime d = stayStart; d <= stayEnd; d = d.AddDays(1))
            {
                int currentWeek = ISOWeek.GetWeekOfYear(d);
                if (currentWeek == 53) currentWeek = 52; // Fallback handling for years containing 53 weeks. 

                if (currentWeek == session.WeekNumber)
                {
                    isCorrectWeek = true;
                    break;
                }
            }

            if (!isCorrectWeek)
            {
                return (false, $"Kunden är inbokad {stayStart:yyyy-MM-dd} till {stayEnd:yyyy-MM-dd}. Skidskolan (vecka {session.WeekNumber}) måste bokas under kundens aktiva vistelse.");
            }

            if (!isCorrectWeek)
            {
                return (false, $"Kunden är inte inbokad under vecka {session.WeekNumber}. Skidskolan måste bokas under kundens aktiva vistelse.");
            }

            // Validate that the session is not already full
            int currentParticipants = _unitOfWork.SkiLesson.GetBookedParticipantCount(sessionId);
            if (currentParticipants >= lesson.MaxParticipants)
            {
                return (false, "Det valda passet är tyvärr fullbokat.");
            }

            // Create and persist the lesson booking relation
            var lessonBooking = new SkiLessonBooking
            {
                BookingID = bookingId,
                SkiLessonSessionID = sessionId
            };

            // Calculate corporate discount
            decimal rawPrice = lesson.Price;
            decimal discount = 0m;

            if (booking.Customer is CorporateCustomer corp && corp.DiscountRate > 0)
            {
                discount = Math.Round(rawPrice * corp.DiscountRate, 2, MidpointRounding.AwayFromZero);
                booking.DiscountAmount += discount;
            }

            decimal finalPrice = rawPrice - discount;

            // Add lesson price to the booking total amount
            booking.TotalAmount += finalPrice;
            booking.SkiLessons.Add(lessonBooking);
            _unitOfWork.Booking.Update(booking);

            session.Participants.Add(lessonBooking);
            _unitOfWork.SkiLesson.Update(lesson);

            if (_unitOfWork.Complete() > 0)
            {
                return (true, $"Lektionen är bokad! En plats i {lesson.Level} (v. {session.WeekNumber}) är tillagd på bokning #{bookingId}.");
            }

            return (false, "Ett oväntat fel uppstod när bokningen skulle sparas i databasen.");
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
                decimal rawPrice = lesson.Price;
                decimal discount = (booking.Customer is CorporateCustomer corp && corp.DiscountRate > 0)
                    ? Math.Round(rawPrice * corp.DiscountRate, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                booking.TotalAmount = Math.Max(0, booking.TotalAmount - (rawPrice - discount));
                booking.DiscountAmount = Math.Max(0, booking.DiscountAmount - discount);
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

        /// <summary>
        /// Gets information regarding every customer booked to a session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public string GetFormattedParticipantList(int sessionId)
        {
            var session = _unitOfWork.SkiLesson.GetSessionWithParticipantDetails(sessionId);

            if (session == null)
            {
                return "Kunde inte hitta skidskolepasset.";
            }

            if (session.Participants == null || !session.Participants.Any())
            {
                return "Det finns inga bokade elever på detta pass ännu.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Bokade elever för {session.SkiLesson.Level} (V.{session.WeekNumber}, {session.TimeSlot}):\n");

            foreach (var participant in session.Participants)
            {
                string customerName = participant.Booking?.Customer?.DisplayName ?? "Okänd kund";

                sb.AppendLine($"• Bokning #{participant.BookingID} - {customerName}");
            }

            return sb.ToString();
        }
    }
}