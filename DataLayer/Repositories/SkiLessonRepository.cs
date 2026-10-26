using DataLayer.Interfaces;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class SkiLessonRepository : Repository<SkiLesson>, ISkiLessonRepository
    {
        private readonly SkiCenterDbContext _context;

        public SkiLessonRepository(SkiCenterDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all ski lessons matching a specific difficulty level.
        /// </summary>
        /// <param name="level">The skill level (e.g., Green, Blue, Red, Black, Private).</param>
        public IEnumerable<SkiLesson> GetLessonsByLevel(string level)
        {
            return _context.Set<SkiLesson>()
                .Where(lesson => lesson.Level == level)
                .ToList();
        }

        /// <summary>
        /// Gets all ski lessons with corresponding ski lesson sessions.
        /// </summary>
        public IEnumerable<SkiLesson> GetLessonsWithSessions()
        {
            return _context.Set<SkiLesson>()
                .Include(lesson => lesson.Sessions)
                    .ThenInclude(session => session.Participants)
                .ToList();
        }

        /// <summary>
        /// Gets a specific ski lesson by its level and scheduled days.
        /// </summary>
        /// <param name="level">The skill level.</param>
        /// <param name="days">The scheduled days (e.g., mån-ons, tor-fre).</param>
        public SkiLesson? GetLessonByLevelAndDays(string level, string days)
        {
            return _context.Set<SkiLesson>()
                .FirstOrDefault(lesson => lesson.Level == level && lesson.Days == days);
        }

        /// <summary>
        /// Gets a ski lesson by its identifier including all associated sessions and participants.
        /// </summary>
        /// <param name="skiLessonId">The ski lesson identifier.</param>
        public SkiLesson? GetLessonWithSessionsAndBookings(int skiLessonId)
        {
            return _context.Set<SkiLesson>()
                .Include(lesson => lesson.Sessions)
                    .ThenInclude(session => session.Participants)
                .FirstOrDefault(lesson => lesson.SkiLessonID == skiLessonId);
        }

        /// <summary>
        /// Gets all ski lessons with their sessions and bookings for a specific calendar week.
        /// </summary>
        /// <param name="weekNumber">The calendar week number.</param>
        public IEnumerable<SkiLesson> GetLessonsWithSessionsByWeek(int weekNumber)
        {
            return _context.Set<SkiLesson>()
                .Include(lesson => lesson.Sessions.Where(s => s.WeekNumber == weekNumber))
                    .ThenInclude(session => session.Participants)
                .ToList();
        }

        /// <summary>
        /// Gets all sessions for a specific week and level where the participant limit has not been reached.
        /// </summary>
        /// <param name="weekNumber">The calendar week number.</param>
        /// <param name="level">The skill level.</param>
        public IEnumerable<SkiLessonSession> GetAvailableSessions(int weekNumber, string level)
        {
            return _context.Set<SkiLessonSession>()
                .Include(session => session.SkiLesson)
                .Include(session => session.Participants)
                .Where(session => session.WeekNumber == weekNumber &&
                                  session.SkiLesson.Level == level &&
                                  session.Participants.Count < session.SkiLesson.MaxParticipants)
                .ToList();
        }

        /// <summary>
        /// Gets the current number of booked participants for a specific session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public int GetBookedParticipantCount(int sessionId)
        {
            return _context.Set<SkiLessonBooking>()
                .Count(booking => booking.SkiLessonSessionID == sessionId);
        }

        /// <summary>
        /// Gets every booked customers information for viewing of participants for each session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public SkiLessonSession? GetSessionWithParticipantDetails(int sessionId)
        {
            return _context.Set<SkiLessonSession>()
                .Include(s => s.SkiLesson)
                .Include(s => s.Participants)
                    .ThenInclude(p => p.Booking)
                        .ThenInclude(b => b.Customer)
                .FirstOrDefault(s => s.SkiLessonSessionID == sessionId);
        }
    }
}