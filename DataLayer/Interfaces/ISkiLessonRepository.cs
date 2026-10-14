using EntityLayer;

namespace DataLayer.Interfaces
{
    public interface ISkiLessonRepository : IRepository<SkiLesson>
    {
        /// <summary>
        /// Gets all ski lessons matching a specific difficulty level.
        /// </summary>
        /// <param name="level">The skill level (e.g., Green, Blue, Red, Black, Private).</param>
        IEnumerable<SkiLesson> GetLessonsByLevel(string level);

        /// <summary>
        /// Gets a specific ski lesson by its level and scheduled days.
        /// </summary>
        /// <param name="level">The skill level.</param>
        /// <param name="days">The scheduled days (e.g., mån-ons, tor-fre).</param>
        SkiLesson GetLessonByLevelAndDays(string level, string days);

        /// <summary>
        /// Gets a ski lesson by its identifier including all associated sessions and participants.
        /// </summary>
        /// <param name="skiLessonId">The ski lesson identifier.</param>
        SkiLesson GetLessonWithSessionsAndBookings(int skiLessonId);

        /// <summary>
        /// Gets all ski lessons with their sessions and bookings for a specific calendar week.
        /// </summary>
        /// <param name="weekNumber">The calendar week number.</param>
        IEnumerable<SkiLesson> GetLessonsWithSessionsByWeek(int weekNumber);

        /// <summary>
        /// Gets all sessions for a specific week and level where the participant limit has not been reached.
        /// </summary>
        /// <param name="weekNumber">The calendar week number.</param>
        /// <param name="level">The skill level.</param>
        IEnumerable<SkiLessonSession> GetAvailableSessions(int weekNumber, string level);

        /// <summary>
        /// Gets all lessons with coupled ski lesson sessions. 
        /// </summary>
        IEnumerable<SkiLesson> GetLessonsWithSessions();

        /// <summary>
        /// Gets the current number of booked participants for a specific session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        int GetBookedParticipantCount(int sessionId);
    }
}