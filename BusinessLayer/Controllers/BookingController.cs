
using DataLayer.Interfaces;
using EntityLayer;
using System.Globalization;

namespace BusinessLayer
{
    public class BookingController 
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves all bookings with their navigation properties.
        /// (Customer, Accommodations, MeetingRooms, SkiLessons, Rentals and Invoice).
        /// </summary>
        /// <returns>A collection of all bookings.</returns>
        public IEnumerable<Booking> GetAllBookings()
        {
            return _unitOfWork.Booking.GetAllBookings();
        }

        /// <summary>
        /// Retrieves a specific booking with all details by its unique BookingID.
        /// </summary>
        /// <param name="bookingID">The unique booking ID.</param>
        /// <returns>The complete booking entity, or null if not found.</returns>
        public Booking GetSpecificBooking(int bookingID)
        {
            if (bookingID <= 0) return null;

            return _unitOfWork.Booking.GetSpecificBooking(bookingID);
        }

        /// <summary>
        /// Retrieves all bookings that fall within a specified date interval.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>A collection of matching bookings.</returns>
        public IEnumerable<Booking> GetBookingsInRange(DateTime startDate, DateTime endDate)
        {
            if (startDate == DateTime.MinValue ||
                endDate == DateTime.MinValue ||
                startDate >= endDate)
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsInRange(startDate, endDate);
        }

        /// <summary>
        /// Retrieves all bookings filtered by status.
        /// (e.g., Preliminary, Confirmed, Cancelled).
        /// </summary>
        /// <param name="status">The booking status.</param>
        /// <returns>A collection of bookings with the specified status.</returns>
        public IEnumerable<Booking> GetBookingsByStatus(BookingStatus status)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), status))
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsByStatus(status);
        }

        /// <summary>
        /// Retrieves booking history for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer's ID.</param>
        /// <returns>A collection of bookings belonging to the customer.</returns>
        public IEnumerable<Booking> GetBookingsByCustomer(int customerId)
        {
            if (customerId <= 0)
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsByCustomer(customerId);
        }

        /// <summary>
        /// Retrieves available accommodations for the selected dates
        /// and number of guests.
        /// </summary>
        /// <param name="startDate">The check-in date.</param>
        /// <param name="endDate">The check-out date.</param>
        /// <param name="numberOfGuests">The number of guests.</param>
        /// <returns>A collection of available accommodations.</returns>
        public IEnumerable<Accommodation> GetAvailableAccommodations(
            DateTime startDate,
            DateTime endDate,
            int numberOfGuests)
        {
            if (startDate.Date < DateTime.Today ||
                startDate >= endDate ||
                numberOfGuests <= 0)
            {
                return Enumerable.Empty<Accommodation>();
            }

            return _unitOfWork.Accommodation.GetAvailableAccommodations(
                startDate,
                endDate,
                numberOfGuests);
        }


        /// <summary>
        /// Calculates the accommodation price using existing season prices.
        /// Uses weekly prices for full seven-night periods and
        /// nightly prices for remaining nights.
        /// </summary>
        public decimal CalculateAccommodationPrice(
            int accommodationTypeId,
            DateTime startDate,
            DateTime endDate)
        {
            if (accommodationTypeId <= 0 ||
                startDate.Date >= endDate.Date)
            {
                throw new ArgumentException(
                    "Ogiltig boendetyp eller bokningsperiod.");
            }

            decimal totalPrice = 0;
            DateTime currentDate = startDate.Date;
            DateTime checkoutDate = endDate.Date;

            // Calculate full seven-night periods.
            while ((checkoutDate - currentDate).Days >= 7)
            {
                int week = ISOWeek.GetWeekOfYear(currentDate);

                SeasonPrice weeklyPrice =
                    _unitOfWork.SeasonPrice.GetSeasonPrice(
                        accommodationTypeId,
                        "vecka",
                        week);

                if (weeklyPrice == null)
                {
                    // Calculate individual nights if weekly price is missing.
                    break;
                }

                totalPrice += weeklyPrice.Price;
                currentDate = currentDate.AddDays(7);
            }

            // Calculate remaining nights.
            while (currentDate < checkoutDate)
            {
                int week = ISOWeek.GetWeekOfYear(currentDate);

                bool isWeekend =
                    currentDate.DayOfWeek == DayOfWeek.Friday ||
                    currentDate.DayOfWeek == DayOfWeek.Saturday;

                string priceType = isWeekend
                    ? "fre-sön/dygn"
                    : "sön-fre/dygn";

                SeasonPrice nightlyPrice =
                    _unitOfWork.SeasonPrice.GetSeasonPrice(
                        accommodationTypeId,
                        priceType,
                        week);

                // Camping uses standard daily prices.
                // Also used when weekday/weekend prices are unavailable.
                if (nightlyPrice == null)
                {
                    nightlyPrice =
                        _unitOfWork.SeasonPrice.GetSeasonPrice(
                            accommodationTypeId,
                            "dygn",
                            week);
                }

                if (nightlyPrice == null)
                {
                    throw new InvalidOperationException(
                        $"Inget dygnspris finns registrerat för vecka {week}.");
                }

                totalPrice += nightlyPrice.Price;
                currentDate = currentDate.AddDays(1);
            }

            return totalPrice;
        }

        /// <summary>
        /// Calculates the accommodation discount for a corporate customer.
        /// DiscountRate is stored as a decimal, for example 0.10 for 10%.
        /// </summary>
        public decimal CalculateCorporateDiscount(
            CorporateCustomer customer,
            decimal accommodationPrice)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (accommodationPrice < 0)
            {
                throw new ArgumentException("Boendepriset kan inte vara negativt.");
            }

            if (customer.DiscountRate < 0 || customer.DiscountRate > 1)
            {
                throw new InvalidOperationException(
                    "Företagskundens rabatt måste vara mellan 0 och 1.");
            }

            return Math.Round(
                accommodationPrice * customer.DiscountRate,
                2,
                MidpointRounding.AwayFromZero);
        }


        /// <summary>
        /// Calculates an 8% accommodation discount for private customers
        /// when at least 365 days have passed since their latest booking.
        /// </summary>
        public decimal CalculatePrivateCustomerDiscount(
            PrivateCustomer customer,
            decimal accommodationPrice,
            DateTime bookingDate)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (accommodationPrice < 0)
            {
                throw new ArgumentException(
                    "Boendepriset kan inte vara negativt.");
            }

            // Get previous bookings, excluding cancelled bookings.
            var previousBookings = GetBookingsByCustomer(customer.CustomerID)
                .Where(b =>
                    b.BookingDate < bookingDate &&
                    b.BookingStatus != BookingStatus.Cancelled)
                .ToList();

            if (!previousBookings.Any())
            {
                return 0m;
            }

            // Find the date of the most recent booking.
            DateTime latestBookingDate = previousBookings
                .Max(b => b.BookingDate);

            // Check whether at least 365 days have passed.
            if ((bookingDate.Date - latestBookingDate.Date).TotalDays >= 365)
            {
                return Math.Round(
                    accommodationPrice * 0.08m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            return 0m;
        }


        /// <summary>
        /// Creates a new accommodation booking and saves it to the database.
        /// </summary>
        public Booking CreateBooking(
            Customer customer,
            Accommodation accommodation,
            DateTime startDate,
            DateTime endDate,
            int numberOfGuests,
            bool hasCancellationInsurance)
        {
            if (customer == null || customer.CustomerID <= 0)
            {
                throw new ArgumentException("Välj en giltig kund.");
            }

            if (accommodation == null || accommodation.AccommodationID <= 0)
            {
                throw new ArgumentException("Välj ett giltigt boende.");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            if (startDate < DateTime.Today || endDate <= startDate)
            {
                throw new ArgumentException("Ange en giltig bokningsperiod.");
            }

            if (numberOfGuests <= 0)
            {
                throw new ArgumentException("Ange ett giltigt antal gäster.");
            }

            // Check that the accommodation is still available.
            bool isAvailable = GetAvailableAccommodations(
                startDate,
                endDate,
                numberOfGuests)
                .Any(a => a.AccommodationID == accommodation.AccommodationID);

            if (!isAvailable)
            {
                throw new InvalidOperationException(
                    "Boendet är inte längre tillgängligt för den valda perioden.");
            }

            // Calculate the accommodation price.
            decimal accommodationPrice = CalculateAccommodationPrice(
                accommodation.AccommodationTypeID,
                startDate,
                endDate);

            DateTime bookingDate = DateTime.Now;
            decimal discountAmount = 0m;
            decimal discountRate = 0m;

            // Calculate the discount based on customer type.
            if (customer is PrivateCustomer privateCustomer)
            {
                discountAmount = CalculatePrivateCustomerDiscount(
                    privateCustomer,
                    accommodationPrice,
                    bookingDate);

                if (discountAmount > 0)
                {
                    discountRate = 0.08m;
                }
            }
            else if (customer is CorporateCustomer corporateCustomer)
            {
                discountAmount = CalculateCorporateDiscount(
                    corporateCustomer,
                    accommodationPrice);

                discountRate = corporateCustomer.DiscountRate;
            }

            decimal finalAccommodationPrice =
                accommodationPrice - discountAmount;

            decimal insurancePrice =
                hasCancellationInsurance ? 300m : 0m;

            decimal totalAmount =
                finalAccommodationPrice + insurancePrice;

            // Create the accommodation connection.
            BookingAccommodation bookingAccommodation =
                new BookingAccommodation
                {
                    AccommodationID = accommodation.AccommodationID,
                    StartDate = startDate,
                    EndDate = endDate,
                };

            // Create the booking.
            Booking booking = new Booking
            {
                CustomerID = customer.CustomerID,
                BookingDate = bookingDate,
                BookingStatus = BookingStatus.Preliminary,
                CheckInStatus = CheckInStatus.NotCheckedIn,
                HasCancellationInsurance = hasCancellationInsurance,
                TotalAmount = totalAmount,
                Accommodations = new List<BookingAccommodation>
                {
                    bookingAccommodation
                }
            };

            // Save the booking and its accommodation connection.
            _unitOfWork.Booking.Add(booking);
            _unitOfWork.Complete();

            return booking;
        }

        /// <summary>
        /// Updates the check-in status of a specific booking.
        /// </summary>
        /// <param name="bookingId">The unique booking identifier.</param>
        /// <param name="newStatus">The new check-in status to apply.</param>
        /// <returns>True if the status was successfully updated; otherwise, false.</returns>
        public bool UpdateCheckInStatus(int bookingId, CheckInStatus newStatus)
        {
            if (bookingId <= 0)
            {
                return false;
            }

            var booking = _unitOfWork.Booking.GetSpecificBooking(bookingId)
                          ?? _unitOfWork.Booking.GetByID(bookingId);

            if (booking == null)
            {
                return false;
            }

            booking.CheckInStatus = newStatus;

            // If Guest checks in
            if (newStatus == CheckInStatus.CheckedIn)
            {
                booking.BookingStatus = BookingStatus.Confirmed;
            }
            // When Guest checks out
            else if (newStatus == CheckInStatus.CheckedOut)
            {
                booking.BookingStatus = BookingStatus.Finished;
            }

            _unitOfWork.Booking.Update(booking);
            return _unitOfWork.Complete() > 0;
        }

        /*
        /// <summary>
        /// Checks whether a booking is fully paid based on its invoices.
        /// </summary>
        /// <param name="bookingId">The unique booking identifier.</param>
        /// <returns>True if all invoices are paid or no invoices exist; otherwise, false.</returns>
        public bool IsBookingFullyPaid(int bookingId)
        {
            var booking = _unitOfWork.Booking.GetSpecificBooking(bookingId)
                          ?? _unitOfWork.Booking.GetByID(bookingId);

            if (booking == null)
            {
                return false;
            }

            // Företagskunder faktureras i efterhand och tillåts checka in
            if (booking.Customer is CorporateCustomer)
            {
                return true;
            }

            if (booking.Invoices == null || !booking.Invoices.Any())
            {
                return true;
            }

            return booking.Invoices.All(inv => inv.IsPaid);
        }
        */


        /// <summary>
        /// Cancels a booking while keeping it in the customer's history.
        /// Does not process refunds or send confirmation emails.
        /// </summary>
        public Booking CancelBooking(int bookingID)
        {
            if (bookingID <= 0)
            {
                throw new ArgumentException("Ogiltigt bokningsnummer.");
            }

            Booking booking =
                _unitOfWork.Booking.GetSpecificBooking(bookingID);

            if (booking == null)
            {
                throw new InvalidOperationException(
                    "Bokningen kunde inte hittas.");
            }

            if (booking.BookingStatus == BookingStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Bokningen är redan avbokad.");
            }

            if (booking.BookingStatus == BookingStatus.Finished ||
                booking.CheckInStatus != CheckInStatus.NotCheckedIn)
            {
                throw new InvalidOperationException(
                    "En påbörjad eller avslutad bokning kan inte avbokas.");
            }

            BookingStatus previousStatus = booking.BookingStatus;

            try
            {
                // Change the status instead of deleting the booking.
                booking.BookingStatus = BookingStatus.Cancelled;
                _unitOfWork.Complete();

                return booking;
            }
            catch
            {
                // Restore the tracked status if saving fails.
                booking.BookingStatus = previousStatus;
                throw;
            }
        }

        /// <summary>
        /// Calculates the protected cancellation refund for accommodation bookings.
        /// Returns null when the applicable refund rule cannot be determined.
        /// </summary>
        public decimal? GetCancellationRefund(
            Booking booking,
            DateTime cancellationDate)
        {
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            // No refund applies when cancellation protection was not purchased.
            if (!booking.HasCancellationInsurance)
            {
                return 0m;
            }

            // An arrival date is needed to check protection eligibility.
            if (!booking.Accommodations.Any())
            {
                return null;
            }

            DateTime arrivalDate = booking.Accommodations
                .Min(stay => stay.StartDate)
                .Date;

            // Protection applies up to and including five days before arrival.
            if (cancellationDate.Date > arrivalDate.AddDays(-5))
            {
                return null;
            }

            const decimal protectionFee = 300m;

            if (booking.TotalAmount < protectionFee)
            {
                throw new InvalidOperationException(
                    "Bokningsbeloppet är lägre än avgiften för avbeställningsskyddet.");
            }

            // The protection fee is excluded from the refundable amount.
            return Math.Round(
                (booking.TotalAmount - protectionFee) * 0.80m,
                2,
                MidpointRounding.AwayFromZero);
        }


    }
}
