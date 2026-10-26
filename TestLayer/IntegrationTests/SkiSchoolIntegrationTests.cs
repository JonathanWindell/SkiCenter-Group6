using BusinessLayer.Controllers;
using DataLayer;
using EntityLayer;
using Microsoft.EntityFrameworkCore;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class SkiSchoolIntegrationTests
    {
        private SkiCenterDbContext _context;
        private SkiSchoolController _skiSchoolController;

        private List<SkiLessonBooking> _createdLessonBookings;
        private List<Booking> _createdBookings;
        private List<Customer> _createdCustomers;

        [SetUp]
        public void Setup()
        {
            _context = new SkiCenterDbContext();

            var unitOfWork = new UnitOfWork(_context);
            _skiSchoolController = new SkiSchoolController(unitOfWork);

            _createdLessonBookings = new List<SkiLessonBooking>();
            _createdBookings = new List<Booking>();
            _createdCustomers = new List<Customer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                // Clear ChangeTracker to prevent state collision
                _context.ChangeTracker.Clear();

                // Remove lesson bookings (child)
                foreach (var lb in _createdLessonBookings)
                {
                    var existing = _context.Set<SkiLessonBooking>().Find(lb.SkiLessonBookingID);
                    if (existing != null)
                    {
                        _context.Set<SkiLessonBooking>().Remove(existing);
                    }
                }
                _context.SaveChanges();

                // Remove test bookings
                foreach (var b in _createdBookings)
                {
                    var existing = _context.Set<Booking>().Find(b.BookingID);
                    if (existing != null)
                    {
                        _context.Set<Booking>().Remove(existing);
                    }
                }
                _context.SaveChanges();

                // Remove test customers (parent)
                foreach (var c in _createdCustomers)
                {
                    var existing = _context.Set<Customer>().Find(c.CustomerID);
                    if (existing != null)
                    {
                        _context.Set<Customer>().Remove(existing);
                    }
                }
                _context.SaveChanges();

                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        /// Verifies that a student can be successfully booked into an available ski lesson session,
        /// ensuring the method returns true, the booking total amount is updated, and the record is persisted.
        /// </summary>
        [Test]
        public void AvailableLessonSession_AddStudent_ReturnsTrue()
        {
            // Arrange: Create and persist an isolated test customer
            var customer = new PrivateCustomer
            {
                FirstName = "Test-Joakim",
                LastName = "Johansson",
                Email = "Joakimg.Johansson@gmail.com",
                PhoneNumber = "0761636787",
                Address = "Bokgatan 12",
                CreditLimit = 12000m
            };
            _context.Set<Customer>().Add(customer);
            _context.SaveChanges();
            _createdCustomers.Add(customer);

            // Arrange: Create and persist an active booking for the customer
            var booking = new Booking
            {
                CustomerID = customer.CustomerID,
                BookingDate = DateTime.Now,
                BookingStatus = BookingStatus.Confirmed,
                CheckInStatus = CheckInStatus.NotCheckedIn,
                HasCancellationInsurance = false,
                TotalAmount = 0m
            };
            _context.Set<Booking>().Add(booking);
            _context.SaveChanges();
            _createdBookings.Add(booking);

            // Arrange: Retrieve a seeded session with remaining capacity
            var session = _context.Set<SkiLessonSession>()
                .Include(s => s.SkiLesson)
                .Include(s => s.Participants)
                .FirstOrDefault(s => s.Participants.Count < s.SkiLesson.MaxParticipants);

            Assert.That(session, Is.Not.Null, "A seeded ski lesson session with available spots must exist.");

            decimal initialAmount = booking.TotalAmount;
            decimal expectedLessonPrice = session.SkiLesson.Price;

            // Act
            var result = _skiSchoolController.BookParticipant(booking.BookingID, session.SkiLessonSessionID);

            // Assert: Verify operation result
            Assert.That(result, Is.True, "BookParticipant should return true when spots are available.");

            SkiLessonBooking persistedLessonBooking;

            // Assert: Verify database persistence in a detached query
            using (var verifyContext = new SkiCenterDbContext())
            {
                persistedLessonBooking = verifyContext.Set<SkiLessonBooking>()
                    .FirstOrDefault(b => b.BookingID == booking.BookingID && b.SkiLessonSessionID == session.SkiLessonSessionID);

                Assert.That(persistedLessonBooking, Is.Not.Null, "The ski lesson booking record must be saved in the database.");
                _createdLessonBookings.Add(persistedLessonBooking);

                var updatedBooking = verifyContext.Set<Booking>().Find(booking.BookingID);
                Assert.That(updatedBooking, Is.Not.Null);
                Assert.That(updatedBooking.TotalAmount, Is.EqualTo(initialAmount + expectedLessonPrice),
                    "The parent booking total amount should reflect the added lesson price.");
            }

            // Cleanup: Explicit removal within the test in reverse relational order
            var lessonBookingToRemove = _context.Set<SkiLessonBooking>().Find(persistedLessonBooking.SkiLessonBookingID);
            if (lessonBookingToRemove != null)
            {
                _context.Set<SkiLessonBooking>().Remove(lessonBookingToRemove);
                _context.SaveChanges();
                _createdLessonBookings.Remove(persistedLessonBooking);
            }

            var bookingToRemove = _context.Set<Booking>().Find(booking.BookingID);
            if (bookingToRemove != null)
            {
                _context.Set<Booking>().Remove(bookingToRemove);
                _context.SaveChanges();
                _createdBookings.Remove(booking);
            }

            var customerToRemove = _context.Set<Customer>().Find(customer.CustomerID);
            if (customerToRemove != null)
            {
                _context.Set<Customer>().Remove(customerToRemove);
                _context.SaveChanges();
                _createdCustomers.Remove(customer);
            }
        }

        /// <summary>
        /// Verifies that BookParticipant refuses to add a participant once MaxParticipants is reached in the database.
        /// </summary>
        [Test]
        public void FullLessonSession_AddStudent_ReturnsFalse()
        {
            // Arrange: Create a customer and booking
            var customer = new PrivateCustomer
            {
                FirstName = "Test-Joakim",
                LastName = "Johansson",
                Email = "Joakimg.Johansson@gmail.com",
                PhoneNumber = "0761636787",
                Address = "Bokgatan 12",
                CreditLimit = 12000m
            };
            _context.Set<Customer>().Add(customer);
            _context.SaveChanges();
            _createdCustomers.Add(customer);

            var booking = new Booking
            {
                CustomerID = customer.CustomerID,
                BookingDate = DateTime.Now,
                BookingStatus = BookingStatus.Confirmed,
                TotalAmount = 1000m
            };
            _context.Set<Booking>().Add(booking);
            _context.SaveChanges();
            _createdBookings.Add(booking);

            // Arrange: Searches for session with low number of max participants (e.g. Private lessons)
            var session = _context.Set<SkiLessonSession>()
                .Include(s => s.SkiLesson)
                .Include(s => s.Participants)
                .FirstOrDefault(s => s.SkiLesson.MaxParticipants == 2);

            Assert.That(session, Is.Not.Null, "A private lesson session with capacity 2 must exist.");

            var addedFillers = new List<SkiLessonBooking>();

            // Fill session to max number of participants
            while (session.Participants.Count < session.SkiLesson.MaxParticipants)
            {
                var filler = new SkiLessonBooking
                {
                    BookingID = booking.BookingID,
                    SkiLessonSessionID = session.SkiLessonSessionID
                };
                session.Participants.Add(filler);
                _createdLessonBookings.Add(filler);
                addedFillers.Add(filler);
            }
            _context.SaveChanges();

            // Act: Try to book to already full ski lesson session
            var result = _skiSchoolController.BookParticipant(booking.BookingID, session.SkiLessonSessionID);

            // Assert
            Assert.That(result, Is.False, "Cannot book a participant into a session that is already at full capacity.");

            // Cleanup: Explicit removal within the test in reverse relational order
            foreach (var filler in addedFillers)
            {
                var fillerToRemove = _context.Set<SkiLessonBooking>().Find(filler.SkiLessonBookingID);
                if (fillerToRemove != null)
                {
                    _context.Set<SkiLessonBooking>().Remove(fillerToRemove);
                }
                _createdLessonBookings.Remove(filler);
            }
            _context.SaveChanges();

            var bookingToRemove = _context.Set<Booking>().Find(booking.BookingID);
            if (bookingToRemove != null)
            {
                _context.Set<Booking>().Remove(bookingToRemove);
                _context.SaveChanges();
                _createdBookings.Remove(booking);
            }

            var customerToRemove = _context.Set<Customer>().Find(customer.CustomerID);
            if (customerToRemove != null)
            {
                _context.Set<Customer>().Remove(customerToRemove);
                _context.SaveChanges();
                _createdCustomers.Remove(customer);
            }
        }

        /// <summary>
        /// Verifies that canceling an existing ski lesson booking removes the record and reduces the booking amount.
        /// </summary>
        [Test]
        public void BookedParticipant_CancelParticipant_RemovesRecordAndUpdatesPrice()
        {
            // Arrange: Create customer, booking and couple to ski lesson
            var customer = new PrivateCustomer
            {
                FirstName = "Test-Joakim",
                LastName = "Johansson",
                Email = "Joakimg.Johansson@gmail.com",
                PhoneNumber = "0761636787",
                Address = "Bokgatan 12",
                CreditLimit = 12000m
            };
            _context.Set<Customer>().Add(customer);
            _context.SaveChanges();
            _createdCustomers.Add(customer);

            var booking = new Booking
            {
                CustomerID = customer.CustomerID,
                BookingDate = DateTime.Now,
                BookingStatus = BookingStatus.Confirmed,
                TotalAmount = 0m
            };
            _context.Set<Booking>().Add(booking);
            _context.SaveChanges();
            _createdBookings.Add(booking);

            var session = _context.Set<SkiLessonSession>()
                .Include(s => s.SkiLesson)
                .First(s => s.Participants.Count < s.SkiLesson.MaxParticipants);

            // Book ski lesson session spot
            _skiSchoolController.BookParticipant(booking.BookingID, session.SkiLessonSessionID);

            var createdBooking = _context.Set<SkiLessonBooking>()
                .First(b => b.BookingID == booking.BookingID && b.SkiLessonSessionID == session.SkiLessonSessionID);

            // Act: Cancel booking
            bool cancelResult = _skiSchoolController.CancelParticipant(createdBooking.SkiLessonBookingID);

            // Assert
            Assert.That(cancelResult, Is.True, "Canceling an existing participant booking should return true.");

            using (var verifyContext = new SkiCenterDbContext())
            {
                var checkExists = verifyContext.Set<SkiLessonBooking>().Find(createdBooking.SkiLessonBookingID);
                Assert.That(checkExists, Is.Null, "The lesson booking record must be deleted from the database.");

                var checkBooking = verifyContext.Set<Booking>().Find(booking.BookingID);
                Assert.That(checkBooking.TotalAmount, Is.EqualTo(0m), "Total amount should revert to the base price.");
            }

            // Cleanup: Explicit removal within the test (createdBooking is already removed by CancelParticipant)
            var bookingToRemove = _context.Set<Booking>().Find(booking.BookingID);
            if (bookingToRemove != null)
            {
                _context.Set<Booking>().Remove(bookingToRemove);
                _context.SaveChanges();
                _createdBookings.Remove(booking);
            }

            var customerToRemove = _context.Set<Customer>().Find(customer.CustomerID);
            if (customerToRemove != null)
            {
                _context.Set<Customer>().Remove(customerToRemove);
                _context.SaveChanges();
                _createdCustomers.Remove(customer);
            }
        }
    }
}