using DataLayer.Interfaces;
using EntityLayer;
using System.Globalization;

namespace BusinessLayer
{
    public class BookingController
    {
        // Price for cancellation insurance on a booking
        public const decimal CancellationInsurancePrice = 300m;

        private readonly IUnitOfWork _unitOfWork;

        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves all bookings with their navigation properties.
        /// </summary>
        public IEnumerable<Booking> GetAllBookings()
        {
            return _unitOfWork.Booking.GetAllBookings();
        }

        /// <summary>
        /// Retrieves a specific booking with all details.
        /// </summary>
        public Booking GetSpecificBooking(int bookingID)
        {
            if (bookingID <= 0)
                return null;

            return _unitOfWork.Booking.GetSpecificBooking(bookingID);
        }

        /// <summary>
        /// Retrieves bookings within a date interval.
        /// </summary>
        public IEnumerable<Booking> GetBookingsInRange(
            DateTime startDate,
            DateTime endDate)
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
        /// Retrieves bookings by status.
        /// </summary>
        public IEnumerable<Booking> GetBookingsByStatus(BookingStatus status)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), status))
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsByStatus(status);
        }

        /// <summary>
        /// Retrieves bookings for a specific customer.
        /// </summary>
        public IEnumerable<Booking> GetBookingsByCustomer(int customerId)
        {
            if (customerId <= 0)
            {
                return Enumerable.Empty<Booking>();
            }

            return _unitOfWork.Booking.GetBookingsByCustomer(customerId);
        }

        /// <summary>
        /// Retrieves available accommodations for selected dates and guests.
        /// </summary>
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
        /// Retrieves available accommodations when editing an existing booking.
        /// Excludes the current booking from the availability check.
        /// </summary>
        public IEnumerable<Accommodation> GetAvailableAccommodationsForEdit(
            DateTime startDate,
            DateTime endDate,
            int numberOfGuests,
            int bookingId)
        {
            if (startDate.Date <= DateTime.Today ||
                startDate >= endDate ||
                numberOfGuests <= 0 ||
                bookingId <= 0)
            {
                return Enumerable.Empty<Accommodation>();
            }

            Booking booking =
                _unitOfWork.Booking.GetSpecificBooking(bookingId);

            if (booking == null ||
                (booking.BookingStatus != BookingStatus.Preliminary &&
                 booking.BookingStatus != BookingStatus.Confirmed) ||
                booking.CheckInStatus != CheckInStatus.NotCheckedIn ||
                !booking.Accommodations.Any() ||
                booking.Accommodations.Any(a =>
                    a.StartDate.Date <= DateTime.Today))
            {
                return Enumerable.Empty<Accommodation>();
            }

            return _unitOfWork.Accommodation.GetAvailableAccommodationsForEdit(
                startDate,
                endDate,
                numberOfGuests,
                bookingId);
        }

        /// <summary>
        /// Calculates the accommodation price using season prices.
        /// Uses weekly prices for full seven-night periods
        /// and nightly prices for remaining nights.
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

            decimal totalPrice = 0m;
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
                    // Use nightly prices when the weekly price is missing.
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

                // Use standard daily prices when necessary.
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
        /// Calculates the corporate customer discount.
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
                throw new ArgumentException(
                    "Boendepriset kan inte vara negativt.");
            }

            if (customer.DiscountRate < 0 ||
                customer.DiscountRate > 1)
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
        /// Calculates an 8% discount for eligible returning private customers.
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
        /// Creates a new accommodation booking.
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
                throw new ArgumentException(
                    "Välj en giltig kund.");
            }

            if (accommodation == null || accommodation.AccommodationID <= 0)
            {
                throw new ArgumentException(
                    "Välj ett giltigt boende.");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            if (startDate < DateTime.Today || endDate <= startDate)
            {
                throw new ArgumentException(
                    "Ange en giltig bokningsperiod.");
            }

            if (numberOfGuests <= 0)
            {
                throw new ArgumentException(
                    "Ange ett giltigt antal gäster.");
            }

            // Check availability before saving.
            bool isAvailable = GetAvailableAccommodations(
                startDate,
                endDate,
                numberOfGuests)
                .Any(a =>
                    a.AccommodationID == accommodation.AccommodationID);

            if (!isAvailable)
            {
                throw new InvalidOperationException(
                    "Boendet är inte längre tillgängligt för den valda perioden.");
            }

            // Calculate accommodation price.
            decimal accommodationPrice = CalculateAccommodationPrice(
                accommodation.AccommodationTypeID,
                startDate,
                endDate);

            DateTime bookingDate = DateTime.Now;
            decimal discountAmount = 0m;
            decimal discountRate = 0m;

            // Apply the existing customer discount rules.
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
                hasCancellationInsurance
                    ? CancellationInsurancePrice
                    : 0m;

            decimal totalAmount =
                finalAccommodationPrice + insurancePrice;

            // Create the accommodation connection.
            BookingAccommodation bookingAccommodation =
                new BookingAccommodation
                {
                    AccommodationID = accommodation.AccommodationID,
                    StartDate = startDate,
                    EndDate = endDate
                };

            Booking booking = new Booking
            {
                CustomerID = customer.CustomerID,
                BookingDate = bookingDate,
                BookingStatus = BookingStatus.Preliminary,
                CheckInStatus = CheckInStatus.NotCheckedIn,
                HasCancellationInsurance = hasCancellationInsurance,
                TotalAmount = totalAmount,
                AccommodationListPrice = accommodationPrice,
                DiscountRate = discountRate,
                DiscountAmount = discountAmount,
                Accommodations = new List<BookingAccommodation>
                {
                    bookingAccommodation
                }
            };

            _unitOfWork.Booking.Add(booking);
            _unitOfWork.Complete();

            return booking;
        }

        /// <summary>
        /// Updates the accommodation on an existing booking.
        /// Preserves the prices of additional services and insurance.
        /// </summary>
        public Booking UpdateBookingAccommodation(
            int bookingId,
            int accommodationId,
            DateTime startDate,
            DateTime endDate,
            int numberOfGuests)
        {
            if (bookingId <= 0 || accommodationId <= 0)
            {
                throw new ArgumentException(
                    "Välj en giltig bokning och ett giltigt boende.");
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            if (startDate <= DateTime.Today || endDate <= startDate)
            {
                throw new ArgumentException(
                    "Incheckning måste vara efter dagens datum och utcheckning senare än incheckning.");
            }

            if (numberOfGuests <= 0)
            {
                throw new ArgumentException(
                    "Antal gäster måste vara större än noll.");
            }

            Booking booking =
                _unitOfWork.Booking.GetSpecificBooking(bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException(
                    "Bokningen kunde inte hittas.");
            }

            // Only active, future and not checked-in bookings can be edited.
            if ((booking.BookingStatus != BookingStatus.Preliminary &&
                 booking.BookingStatus != BookingStatus.Confirmed) ||
                booking.CheckInStatus != CheckInStatus.NotCheckedIn ||
                !booking.Accommodations.Any() ||
                booking.Accommodations.Any(a =>
                    a.StartDate.Date <= DateTime.Today))
            {
                throw new InvalidOperationException(
                    "Bokningen är påbörjad, incheckad, avslutad eller avbokad och kan inte ändras.");
            }

            if (booking.Accommodations.Count != 1)
            {
                throw new InvalidOperationException(
                    "Ändring av bokningar med flera boenden stöds inte ännu.");
            }

            // Invoiced bookings require separate handling.
            if (booking.Invoices?.Any() == true)
            {
                throw new InvalidOperationException(
                    "Bokningen har fakturor. Kontakta ekonomiansvarig innan ändring.");
            }

            var stay = booking.Accommodations.Single();

            // Check for additional services.
            bool hasAdditionalServices =
                booking.Rentals?.Any() == true ||
                booking.SkiLessons?.Any() == true ||
                booking.MeetingRooms?.Any() == true;

            // Preserve stay dates when additional services exist.
            if (hasAdditionalServices &&
                (startDate != stay.StartDate.Date ||
                 endDate != stay.EndDate.Date))
            {
                throw new InvalidOperationException(
                    "Bokningen innehåller andra tjänster. " +
                    "Du kan byta boende, men måste behålla samma datum. " +
                    "Ändra eller avboka de andra tjänsterna separat innan du ändrar perioden.");
            }

            // Verify accommodation availability.
            Accommodation selectedAccommodation =
                GetAvailableAccommodationsForEdit(
                    startDate,
                    endDate,
                    numberOfGuests,
                    bookingId)
                .FirstOrDefault(a =>
                    a.AccommodationID == accommodationId);

            if (selectedAccommodation == null)
            {
                throw new InvalidOperationException(
                    "Boendet är inte längre ledigt eller har för få bäddar.");
            }

            // Check whether the accommodation and dates are unchanged.
            bool isUnchanged =
                stay.AccommodationID == selectedAccommodation.AccommodationID &&
                stay.StartDate.Date == startDate &&
                stay.EndDate.Date == endDate;

            // Preserve the existing accommodation price if nothing changed.
            decimal newListPrice = isUnchanged
                ? booking.AccommodationListPrice
                : CalculateAccommodationPrice(
                    selectedAccommodation.AccommodationTypeID,
                    startDate,
                    endDate);

            decimal rate = booking.DiscountRate;

            if (rate < 0m || rate > 1m)
            {
                throw new InvalidOperationException(
                    "Bokningens rabattprocent är ogiltig.");
            }

            // Preserve the original discount when nothing changed.
            decimal newDiscount = isUnchanged
                ? booking.DiscountAmount
                : Math.Round(
                    newListPrice * rate,
                    2,
                    MidpointRounding.AwayFromZero);

            // Verify that the original accommodation price is available.
            if (booking.AccommodationListPrice <= 0m ||
                booking.DiscountAmount < 0m ||
                booking.DiscountAmount > booking.AccommodationListPrice)
            {
                throw new InvalidOperationException(
                    "Bokningens ursprungliga boendepris saknas eller är ogiltigt. " +
                    "Priset måste kontrolleras innan bokningen kan ändras.");
            }

            decimal oldAccommodationPrice =
                booking.AccommodationListPrice -
                booking.DiscountAmount;

            decimal newAccommodationPrice =
                newListPrice - newDiscount;

            // Keep existing ski lessons, rentals, meeting rooms and insurance.
            decimal newTotal =
                booking.TotalAmount -
                oldAccommodationPrice +
                newAccommodationPrice;

            if (newTotal < 0m)
            {
                throw new InvalidOperationException(
                    "Det nya totalpriset kunde inte beräknas.");
            }

            // Store original values for recovery if saving fails.
            int oldAccommodationId = stay.AccommodationID;
            Accommodation oldAccommodation = stay.Accommodation;
            DateTime oldStart = stay.StartDate;
            DateTime oldEnd = stay.EndDate;

            decimal oldListPrice = booking.AccommodationListPrice;
            decimal oldDiscount = booking.DiscountAmount;
            decimal oldTotal = booking.TotalAmount;

            try
            {
                // Update the foreign key without clearing the navigation property.
                stay.AccommodationID = selectedAccommodation.AccommodationID;
                stay.StartDate = startDate;
                stay.EndDate = endDate;

                // Update accommodation prices and total.
                booking.AccommodationListPrice = newListPrice;
                booking.DiscountAmount = newDiscount;
                booking.TotalAmount = newTotal;

                _unitOfWork.Complete();

                return booking;
            }
            catch
            {
                // Restore the original tracked values.
                stay.AccommodationID = oldAccommodationId;
                stay.Accommodation = oldAccommodation;
                stay.StartDate = oldStart;
                stay.EndDate = oldEnd;

                booking.AccommodationListPrice = oldListPrice;
                booking.DiscountAmount = oldDiscount;
                booking.TotalAmount = oldTotal;

                throw;
            }
        }

        /// <summary>
        /// Updates the check-in status of a booking.
        /// </summary>
        public bool UpdateCheckInStatus(
            int bookingId,
            CheckInStatus newStatus)
        {
            if (bookingId <= 0)
            {
                return false;
            }

            var booking =
                _unitOfWork.Booking.GetSpecificBooking(bookingId)
                ?? _unitOfWork.Booking.GetByID(bookingId);

            if (booking == null)
            {
                return false;
            }

            booking.CheckInStatus = newStatus;

            if (newStatus == CheckInStatus.CheckedIn)
            {
                booking.BookingStatus = BookingStatus.Confirmed;
            }
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
        public bool IsBookingFullyPaid(int bookingId)
        {
            var booking =
                _unitOfWork.Booking.GetSpecificBooking(bookingId)
                ?? _unitOfWork.Booking.GetByID(bookingId);

            if (booking == null)
            {
                return false;
            }

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
        /// </summary>
        public Booking CancelBooking(int bookingID)
        {
            if (bookingID <= 0)
            {
                throw new ArgumentException(
                    "Ogiltigt bokningsnummer.");
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
                // Restore the original status if saving fails.
                booking.BookingStatus = previousStatus;
                throw;
            }
        }

        /// <summary>
        /// Calculates the protected cancellation refund.
        /// </summary>
        public decimal? GetCancellationRefund(
            Booking booking,
            DateTime cancellationDate)
        {
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            if (!booking.HasCancellationInsurance)
            {
                return 0m;
            }

            if (!booking.Accommodations.Any())
            {
                return null;
            }

            DateTime arrivalDate = booking.Accommodations
                .Min(stay => stay.StartDate)
                .Date;

            // Protection applies up to five days before arrival.
            if (cancellationDate.Date > arrivalDate.AddDays(-5))
            {
                return null;
            }

            if (booking.TotalAmount < CancellationInsurancePrice)
            {
                throw new InvalidOperationException(
                    "Bokningsbeloppet är lägre än avgiften för avbeställningsskyddet.");
            }

            return Math.Round(
                (booking.TotalAmount - CancellationInsurancePrice) * 0.80m,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Confirms preliminary bookings for an approved corporate customer.
        /// </summary>
        public int ConfirmPreliminaryBookingsForCustomer(int customerId)
        {
            if (customerId <= 0)
                return 0;

            var preliminaryBookings =
                _unitOfWork.Booking.GetBookingsByCustomer(customerId)
                    .Where(b =>
                        b.BookingStatus == BookingStatus.Preliminary)
                    .ToList();

            int confirmedCount = 0;

            foreach (var booking in preliminaryBookings)
            {
                booking.BookingStatus = BookingStatus.Confirmed;
                _unitOfWork.Booking.Update(booking);
                confirmedCount++;
            }

            if (confirmedCount > 0)
            {
                _unitOfWork.Complete();
            }

            return confirmedCount;
        }

        /// <summary>
        /// Calculates the booking price and VAT for each service.
        /// </summary>
        public (
            decimal TotalInclVat,
            decimal TotalExclVat,
            decimal TotalVat,
            decimal AccomVat,
            decimal OtherVat) BookingPrice(Booking booking)
        {
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            // Accommodation price including VAT.
            decimal netAccom =
                booking.TotalAmount > 0 &&
                booking.AccommodationListPrice > 0
                    ? Math.Max(
                        0,
                        booking.AccommodationListPrice -
                        booking.DiscountAmount)
                    : booking.Accommodations?.Sum(a =>
                        CalculateAccommodationPrice(
                            a.Accommodation?.AccommodationTypeID ?? 0,
                            a.StartDate,
                            a.EndDate)) ?? 0m;

            var accomVatSplit =
                VatRates.SplitVat(netAccom, VatRates.Accommodation);

            // Equipment rental price including VAT.
            decimal rentalTotal =
                booking.Rentals?.Sum(r => r.TotalPrice) ?? 0m;

            var rentalVatSplit =
                VatRates.SplitVat(
                    rentalTotal,
                    VatRates.EquipmentRental);

            // Ski school price including VAT.
            decimal skiLessonTotal =
                booking.SkiLessons?.Sum(sl =>
                    sl.SkiLessonSession?.SkiLesson?.Price ?? 0m)
                ?? 0m;

            var skiLessonVatSplit =
                VatRates.SplitVat(
                    skiLessonTotal,
                    VatRates.SkiLesson);

            // Meeting room price including VAT.
            decimal meetingRoomTotal = 0m;

            if (booking.MeetingRooms != null &&
                booking.MeetingRooms.Any())
            {
                foreach (var mr in booking.MeetingRooms)
                {
                    int week =
                        ISOWeek.GetWeekOfYear(mr.StartDate);

                    int days = Math.Max(
                        1,
                        (mr.EndDate.Date - mr.StartDate.Date).Days);

                    var priceObj =
                        mr.meetingRoom?.SeasonPrices?
                            .FirstOrDefault(p =>
                                p.PriceType == "dygn" &&
                                p.WeekNumber == week);

                    meetingRoomTotal +=
                        (priceObj?.Price ?? 0m) * days;
                }
            }

            var meetingRoomVatSplit =
                VatRates.SplitVat(
                    meetingRoomTotal,
                    VatRates.MeetingRoom);

            // Cancellation insurance price including VAT.
            decimal insuranceTotal =
                booking.HasCancellationInsurance
                    ? CancellationInsurancePrice
                    : 0m;

            var insuranceVatSplit =
                VatRates.SplitVat(
                    insuranceTotal,
                    VatRates.CancellationInsurance);

            // Calculate total including VAT.
            decimal totalInclVat =
                netAccom +
                rentalTotal +
                skiLessonTotal +
                meetingRoomTotal +
                insuranceTotal;

            // Calculate total excluding VAT.
            decimal totalExclVat =
                accomVatSplit.amountExclVat +
                rentalVatSplit.amountExclVat +
                skiLessonVatSplit.amountExclVat +
                meetingRoomVatSplit.amountExclVat +
                insuranceVatSplit.amountExclVat;

            decimal accomVat =
                accomVatSplit.vatAmount;

            decimal otherVat =
                rentalVatSplit.vatAmount +
                skiLessonVatSplit.vatAmount +
                meetingRoomVatSplit.vatAmount +
                insuranceVatSplit.vatAmount;

            decimal totalVat =
                accomVat + otherVat;

            return (
                totalInclVat,
                totalExclVat,
                totalVat,
                accomVat,
                otherVat);
        }
    }
}