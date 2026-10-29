using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class SkiShopController
    {
        #region Fields and constructor

        private readonly IUnitOfWork _unitOfWork;

        public SkiShopController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Equipment register

        /// <summary>
        /// Gets all equipment types and models with their current stock.
        /// </summary>
        public IEnumerable<Equipment> GetAllEquipmentWithStock()
        {
            return _unitOfWork.Equipment.GetAllEquipmentWithStock();
        }

        /// <summary>
        /// Gets a list of all individual items in the system.
        /// </summary>
        public IEnumerable<EquipmentItem> GetAllEquipmentItems()
        {
            return _unitOfWork.EquipmentItem.GetAllEquipmentItems();
        }

        /// <summary>
        /// Gets equipment filtered by its category.
        /// </summary>
        /// <param name="category"></param>
        public IEnumerable<Equipment> GetEquipmentByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<Equipment>();
            }

            return _unitOfWork.Equipment.GetByCategory(category);
        }

        /// <summary>
        /// Gets a specific article when staff scans/input the article number, only fetches rented.
        /// </summary>
        /// /// <param name="articleNumber"/param>
        public EquipmentItem? GetRentedItemByBarcode(string articleNumber)
        {
            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                return null;
            }

            var item = _unitOfWork.EquipmentItem.GetByBarcode(articleNumber.Trim());
            return item?.Status == EquipmentStatus.Rented ? item : null;
        }

        /// <summary>
        /// Gets all available items of a specific model.
        /// </summary>
        /// <param name="equipmentID"></param>
        public IEnumerable<EquipmentItem> GetAvailableItems(int equipmentID)
        {
            if (equipmentID <= 0)
            {
                return Enumerable.Empty<EquipmentItem>();
            }

            return _unitOfWork.EquipmentItem.GetAvailableItems(equipmentID);
        }

        /// <summary>
        /// Updates an item by its article number, updates status and condition. 
        /// </summary>
        /// <param name="articleNumber"/param>
        /// <param name="status"/param>
        /// <param name="condition"/param>
        public bool UpdateEquipmentItem(string articleNumber, int equipmentID, string size, EquipmentStatus status, EquipmentCondition condition)
        {
            if (equipmentID <= 0 || string.IsNullOrWhiteSpace(articleNumber) || string.IsNullOrWhiteSpace(size))
            {
                return false;
            }

            _unitOfWork.EquipmentItem.UpdateEquipmentItem(articleNumber, equipmentID, size, status, condition);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Register a new item for the ski shop.
        /// </summary>
        /// <param name="equipmentId"/param>
        /// <param name="articleNumber"/param>
        /// <param name="size"/param>
        /// <param name="status"/param>
        /// <param name="condition"/param>
        /// <returns>True if the add was successful, otherwise false.</returns>
        public bool AddEquipmentItem(int equipmentId, string articleNumber, string size, EquipmentStatus status, EquipmentCondition condition)
        {
            // Validation
            if (equipmentId <= 0 || string.IsNullOrWhiteSpace(articleNumber) || string.IsNullOrWhiteSpace(size))
            {
                return false;
            }

            // Checks that the entered article number is unique and does not already exists
            var existingItem = _unitOfWork.EquipmentItem.GetByBarcode(articleNumber.Trim());
            if (existingItem != null)
            {
                return false;
            }

            var newItem = new EquipmentItem
            {
                ArticleNumber = articleNumber.Trim(),
                Status = status,
                Condition = condition,
                Size = size.Trim(),
                EquipmentID = equipmentId
            };

            _unitOfWork.EquipmentItem.Add(newItem);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Deletes an item from the ski shop storage. Does not remove it if its currently rented.
        /// </summary>
        /// <param name="articleNumber"/param>
        /// <returns>True if its removed, otherwise false</returns>
        public bool DeleteEquipmentItem(string articleNumber)
        {
            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                return false;
            }

            var item = _unitOfWork.EquipmentItem.GetByBarcode(articleNumber.Trim());
            if (item == null)
            {
                return false;
            }

            if (item.Status == EquipmentStatus.Rented)
            {
                return false;
            }

            _unitOfWork.EquipmentItem.Delete(item);
            return _unitOfWork.Complete() > 0;
        }

        #endregion

        #region Rentals and returns

        /// <summary>
        /// Gets all active rentals in the skishop.
        /// </summary>
        public IEnumerable<Rental> GetActiveRentals()
        {
            return _unitOfWork.Rental.GetActiveRentals();
        }

        /// <summary>
        /// Gets all rentals connected to a specific customer.
        /// </summary>
        /// /// <param name="customerID"/param>
        public IEnumerable<Rental> GetRentalsForCustomer(int customerID)
        {
            if (customerID <= 0)
            {
                return Enumerable.Empty<Rental>();
            }

            return _unitOfWork.Rental.GetRentalsByCustomer(customerID);
        }

        /// <summary>
        /// Gets all rentals connected to a specific booking.
        /// </summary>
        /// /// <param name="bookingID"/param>
        public IEnumerable<Rental> GetRentalsForBooking(int bookingID)
        {
            if (bookingID <= 0)
            {
                return Enumerable.Empty<Rental>();
            }

            return _unitOfWork.Rental.GetRentalsByBooking(bookingID);
        }

        /// <summary>
        /// Creates a new rental with scanned articles and changes their status to rented.
        /// </summary>
        /// <param name="rental"></param>
        /// <param name="scannedArticleNumbers"></param>
        public bool CreateRental(Rental rental, IEnumerable<string> scannedArticleNumbers)
        {
            if (rental == null || scannedArticleNumbers == null || !scannedArticleNumbers.Any())
            {
                return false;
            }

            // EndDate must be after StartDate
            if (rental.EndDate < rental.StartDate)
            {
                return false;
            }

            // StartDate cannot be before todays date
            if (rental.StartDate.Date < DateTime.Today)
            {
                return false;
            }

            var addedEquipmentItems = new List<EquipmentItem>();

            // Adds scanned items to the rental
            foreach (var barcode in scannedArticleNumbers)
            {
                var item = _unitOfWork.EquipmentItem.GetByBarcode(barcode);
                if (item != null && IsItemAvailableForDates(item, rental.StartDate, rental.EndDate))
                {
                    rental.RentalItems.Add(new RentalItem
                    {
                        Rental = rental,
                        EquipmentItem = item,
                        EquipmentItemID = item.EquipmentItemID
                    });

                    addedEquipmentItems.Add(item);

                    if (rental.StartDate.Date <= DateTime.Today)
                    {
                        _unitOfWork.EquipmentItem.UpdateStatusAndCondition(barcode, EquipmentStatus.Rented, item.Condition);
                    }
                }
            }

            if (!rental.RentalItems.Any())
            {
                return false;
            }

            // Snowmobiles require that the caller has set LicenseVerified
            if (RequiresDriverLicense(addedEquipmentItems) && !rental.LicenseVerified)
            {
                return false;
            }

            // Store the rental's price so it can be shown on the invoice later
            int days = CountRentalDays(rental.StartDate, rental.EndDate);
            rental.TotalPrice = addedEquipmentItems
                .Sum(item => CalculateRentalPrice(item.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days) ?? 0m);

            rental.Status = "Active";
            _unitOfWork.Rental.Add(rental);

            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Returns a rental or scanned articles and make them available again. OLD FUNCTION, keep?
        /// </summary>
        /// <param name="rentalID"></param>
        public bool ReturnRental(int rentalID)
        {
            var rental = _unitOfWork.Rental.GetRentalWithItems(rentalID);
            if (rental == null || rental.Status == "Returned")
            {
                return false;
            }

            // Returns status to returned
            foreach (var ri in rental.RentalItems)
            {
                if (ri.EquipmentItem != null)
                {
                    _unitOfWork.EquipmentItem.UpdateStatusAndCondition(ri.EquipmentItem.ArticleNumber, EquipmentStatus.Available, ri.EquipmentItem.Condition);
                }
            }

            rental.Status = "Returned";
            rental.ReturnDate = DateTime.Now;

            _unitOfWork.Rental.Update(rental);
            return _unitOfWork.Complete() > 0;
        }

        #endregion

        #region Booking search and availability

        // Equipment category that requires a verified driver's license
        public const string SnowmobileCategory = "Skoter";

        /// <summary>
        /// Finds active room bookings by booking number or room number.
        /// A numeric search text is matched against both booking ID and room number.
        /// </summary>
        /// <param name="searchText">Booking number, e.g. "12", or room number, e.g. "LI1".</param>
        public IEnumerable<Booking> FindActiveBookings(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return Enumerable.Empty<Booking>();
            }

            string text = searchText.Trim();
            int? bookingID = int.TryParse(text, out int parsedID) && parsedID > 0 ? parsedID : null;

            return _unitOfWork.Booking.FindActiveBookings(bookingID, text);
        }

        /// <summary>
        /// Gets all available items with their price lists, optionally for one category.
        /// </summary>
        /// <param name="category"></param>
        public IEnumerable<EquipmentItem> GetAvailableItemsForRental(string? category = null)
        {
            return _unitOfWork.EquipmentItem.GetAvailableItemsWithPrices(category);
        }

        #endregion

        #region Rental rules and pricing

        /// <summary>
        /// Returns true if any of the items is a snowmobile, which requires a verified driver's license.
        /// </summary>
        public static bool RequiresDriverLicense(IEnumerable<EquipmentItem> items)
        {
            return items != null && items.Any(item => item.Equipment?.Category == SnowmobileCategory);
        }

        /// <summary>
        /// Number of rental days, counting both the first and the last day.
        /// </summary>
        public static int CountRentalDays(DateTime startDate, DateTime endDate)
        {
            return (endDate.Date - startDate.Date).Days + 1;
        }

        /// <summary>
        /// Calculates the price for renting one item for a number of days using its price list.
        /// Uses the smallest price that covers the number of days. Longer rentals than the price list covers
        /// are priced as the longest period plus the price for the remaining days, e.g. 7 days = 5 days + 2 days.
        /// </summary>
        /// <param name="prices">The equipment's price list.</param>
        /// <param name="days">Number of rental days.</param>
        /// <returns>The price, or null if the equipment has no price list.</returns>
        public static decimal? CalculateRentalPrice(IEnumerable<EquipmentPriceMatrix> prices, int days)
        {
            List<EquipmentPriceMatrix> priceList = prices?.OrderBy(p => p.Days).ToList() ?? new List<EquipmentPriceMatrix>();

            if (days <= 0 || priceList.Count == 0)
            {
                return null;
            }

            EquipmentPriceMatrix? covering = priceList.FirstOrDefault(p => p.Days >= days);
            if (covering != null)
            {
                return covering.TotalAmount;
            }

            EquipmentPriceMatrix longest = priceList.Last();
            return longest.TotalAmount + CalculateRentalPrice(priceList, days - longest.Days);
        }

        #endregion

        #region Add equipment to booking

        /// <summary>
        /// Adds available equipment to an active room booking. The items are linked to a new rental on the booking,
        /// their status changes to Rented, and the rental price is added to the booking's total amount.
        /// Everything is saved in one database transaction.
        /// </summary>
        /// <param name="bookingID">The active booking to add the equipment to.</param>
        /// <param name="equipmentItemIDs">The items to rent out.</param>
        /// <param name="startDate">First rental day.</param>
        /// <param name="endDate">Last rental day.</param>
        /// <param name="licenseVerified">True if staff have verified the customer's driver's license.</param>
        /// <returns>Whether it succeeded, a message for the user, and the amount added to the booking.</returns>
        public (bool success, string message, decimal addedAmount) AddEquipmentToBooking(
            int bookingID, IEnumerable<int> equipmentItemIDs, DateTime startDate, DateTime endDate, bool licenseVerified)
        {
            List<int> itemIDs = equipmentItemIDs?.Distinct().ToList() ?? new List<int>();

            if (bookingID <= 0)
            {
                return (false, "Ingen bokning är vald.", 0m);
            }

            if (itemIDs.Count == 0)
            {
                return (false, "Välj minst en utrustning att hyra ut.", 0m);
            }

            if (endDate.Date < startDate.Date)
            {
                return (false, "Slutdatum kan inte vara före startdatum.", 0m);
            }

            if (startDate.Date < DateTime.Today)
            {
                return (false, "Startdatum kan inte vara före dagens datum.", 0m);
            }

            Booking? activeBooking = _unitOfWork.Booking.FindActiveBookings(bookingID, null).FirstOrDefault();
            if (activeBooking == null)
            {
                return (false, $"Bokning {bookingID} är inte en aktiv rumsbokning.", 0m);
            }

            // The rental period must be within the room stay
            DateTime stayStart = activeBooking.Accommodations.Min(ba => ba.StartDate).Date;
            DateTime stayEnd = activeBooking.Accommodations.Max(ba => ba.EndDate).Date;

            if (startDate.Date < stayStart || endDate.Date > stayEnd)
            {
                return (false, $"Hyresperioden måste ligga inom vistelsen {stayStart:yyyy-MM-dd} – {stayEnd:yyyy-MM-dd}.", 0m);
            }

            List<EquipmentItem> items = _unitOfWork.EquipmentItem.GetItemsByIds(itemIDs).ToList();
            if (items.Count != itemIDs.Count)
            {
                return (false, "En eller flera utrustningar kunde inte hittas.", 0m);
            }

            // Only available equipment may be rented out. This is validated through date check.
            List<EquipmentItem> unavailable = items
                .Where(item => !IsItemAvailableForDates(item, startDate, endDate))
                .ToList();
            if (unavailable.Count > 0)
            {
                string list = string.Join(", ", unavailable.Select(item => $"{item.ArticleNumber} ({StatusText(item.Status)})"));
                return (false, $"Utrustningen är inte tillgänglig: {list}.", 0m);
            }

            bool requiresLicense = RequiresDriverLicense(items);
            if (requiresLicense && !licenseVerified)
            {
                return (false, "Körkort måste vara kontrollerat för att hyra ut skoter.", 0m);
            }

            int days = CountRentalDays(startDate, endDate);
            decimal totalPrice = 0m;

            foreach (EquipmentItem item in items)
            {
                decimal? itemPrice = CalculateRentalPrice(item.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days);
                if (itemPrice == null)
                {
                    return (false, $"Utrustningen {item.ArticleNumber} saknar prislista och kan inte hyras ut.", 0m);
                }

                totalPrice += itemPrice.Value;
            }

            decimal actualAddedAmount = totalPrice;
            Booking booking = _unitOfWork.Booking.GetSpecificBooking(bookingID) ?? _unitOfWork.Booking.GetByID(bookingID);

            if (booking.Customer is CorporateCustomer corp && corp.DiscountRate > 0)
            {
                decimal discount = Math.Round(totalPrice * corp.DiscountRate, 2, MidpointRounding.AwayFromZero);
                actualAddedAmount = totalPrice - discount;
                booking.DiscountAmount += discount;
            }

            booking.TotalAmount += actualAddedAmount;
            _unitOfWork.Booking.Update(booking);

            var rental = new Rental
            {
                BookingID = bookingID,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                ReturnDate = endDate.Date,
                Status = "Active",
                TotalPrice = totalPrice,
                LicenseVerified = requiresLicense && licenseVerified
            };

            foreach (EquipmentItem item in items)
            {
                if (startDate.Date <= DateTime.Today)
                {
                    item.Status = EquipmentStatus.Rented;
                }

                rental.RentalItems.Add(new RentalItem
                {
                    EquipmentItemID = item.EquipmentItemID,
                    EquipmentItem = item
                });
            }

            _unitOfWork.Rental.Add(rental);

            if (_unitOfWork.Complete() <= 0)
            {
                return (false, "Uthyrningen kunde inte sparas.", 0m);
            }

            return (true,
                $"{items.Count} utrustning(ar) har lagts till på bokning {bookingID} för {days} dag(ar). " +
                $"{actualAddedAmount:N2} kr har lagts till på bokningen. Nytt totalbelopp: {booking.TotalAmount:N2} kr.",
                actualAddedAmount);
        }

        #endregion

        #region Return equipment

        /// <summary>
        /// Gets the active rental for the selected item.
        /// </summary>
        public Rental? GetActiveRentalForItem(int equipmentItemID)
        {
            if (equipmentItemID <= 0)
            {
                return null;
            }

            return _unitOfWork.Rental.GetActiveRentals()
                .FirstOrDefault(r => r.RentalItems.Any(ri => ri.EquipmentItemID == equipmentItemID));
        }

        /// <summary>
        /// Makes a return of the articles inside a list, updates condition and sets a complete rental return to "Returned".
        /// </summary>
        public (bool Success, string Message) ReturnMultipleEquipmentItems(
            IEnumerable<(int EquipmentItemID, EquipmentCondition Condition, bool IsDamaged)> returns)
        {
            var returnList = returns?.ToList() ?? new List<(int, EquipmentCondition, bool)>();
            if (returnList.Count == 0)
            {
                return (false, "Inga artiklar att återlämna.");
            }

            var affectedRentalIds = new HashSet<int>();

            foreach (var (itemId, condition, isDamaged) in returnList)
            {
                var item = _unitOfWork.EquipmentItem.GetByID(itemId);
                if (item == null)
                {
                    continue;
                }


                item.Status = isDamaged ? EquipmentStatus.Damaged : EquipmentStatus.Available;
                item.Condition = condition;
                _unitOfWork.EquipmentItem.Update(item);

                var activeRental = GetActiveRentalForItem(itemId);
                if (activeRental != null)
                {
                    affectedRentalIds.Add(activeRental.RentalID);
                }
            }

            foreach (int rentalId in affectedRentalIds)
            {
                var trackedRental = _unitOfWork.Rental.GetRentalWithItems(rentalId);
                if (trackedRental == null) continue;

                bool hasRentedItemsLeft = trackedRental.RentalItems
                    .Any(ri => ri.EquipmentItem != null && ri.EquipmentItem.Status == EquipmentStatus.Rented);

                if (!hasRentedItemsLeft)
                {
                    trackedRental.Status = "Returned";
                    trackedRental.ReturnDate = DateTime.Now;
                    _unitOfWork.Rental.Update(trackedRental);
                }
            }

            int changes = _unitOfWork.Complete();
            if (changes <= 0)
            {
                return (false, "Inga ändringar behövde sparas eller så misslyckades databasuppdateringen.");
            }

            return (true, $"{returnList.Count} artikel/artiklar har återlämnats.");
        }

        #endregion

        #region Availability checks

        /// Validates a specific equipment between dates.
        /// </summary>
        public bool IsItemAvailableForDates(EquipmentItem item, DateTime startDate, DateTime endDate)
        {
            // Checks status for "Damaged" or "Maintenence".
            if (item.Status == EquipmentStatus.Damaged || item.Status == EquipmentStatus.Maintenance)
                return false;

            // If RentalItems has had status = "Rented". 
            if (item.RentalItems == null || !item.RentalItems.Any())
                return true;

            // Check for overlap. Overlas is defined when startdate for one booking begins when one ends. 
            bool isDoubleBooked = item.RentalItems.Any(ri =>
                ri.Rental != null &&
                ri.Rental.Status != "Returned" &&
                ri.Rental.Status != "Cancelled" &&
                ri.Rental.StartDate.Date <= endDate.Date &&
                ri.Rental.EndDate.Date >= startDate.Date
            );

            return !isDoubleBooked;
        }

        private static string StatusText(EquipmentStatus status)
        {
            return status switch
            {
                EquipmentStatus.Available => "tillgänglig",
                EquipmentStatus.Rented => "uthyrd",
                EquipmentStatus.Damaged => "skadad",
                EquipmentStatus.Maintenance => "på service",
                _ => status.ToString()
            };
        }

        #endregion

        #region Edit booking rental

        /// <summary>
        /// Updates articles and dates on a existing rental.
        /// </summary>
        public (bool Success, string Message, decimal PriceDifference) UpdateBookingRental(
            int bookingID,
            int rentalID,
            IEnumerable<int> updatedItemIDs,
            DateTime startDate,
            DateTime endDate,
            bool licenseVerified)
        {
            var itemIDs = updatedItemIDs?.Distinct().ToList() ?? new List<int>();



            if (endDate.Date < startDate.Date)
            {
                return (false, "Slutdatum kan inte vara före startdatum.", 0m);
            }

            // Gets the rental tracked with articles and prices
            var rental = _unitOfWork.Rental.GetRentalWithItems(rentalID);
            if (rental == null || rental.BookingID != bookingID)
            {
                return (false, "Uthyrningsposten kunde inte hittas på bokningen.", 0m);
            }

            var booking = _unitOfWork.Booking.GetSpecificBooking(bookingID);
            if (booking == null)
            {
                return (false, "Bokningen hittades inte.", 0m);
            }

            // If user renoves all items from rental
            if (itemIDs.Count == 0)
            {
                // Returns all items to Available
                foreach (var ri in rental.RentalItems)
                {
                    if (ri.EquipmentItem != null)
                    {
                        ri.EquipmentItem.Status = EquipmentStatus.Available;
                    }
                }
                rental.RentalItems.Clear();

                decimal removedPrice = rental.TotalPrice;
                rental.TotalPrice = 0m;
                rental.Status = "Cancelled";

                // Subtract from totalprice of booking
                decimal actualDeduction = removedPrice;
                if (booking.Customer is CorporateCustomer corp && corp.DiscountRate > 0)
                {
                    decimal discountPart = Math.Round(removedPrice * corp.DiscountRate, 2, MidpointRounding.AwayFromZero);
                    actualDeduction = removedPrice - discountPart;
                    booking.DiscountAmount = Math.Max(0, booking.DiscountAmount - discountPart);
                }

                booking.TotalAmount = Math.Max(0, booking.TotalAmount - actualDeduction);

                _unitOfWork.Rental.Update(rental);
                _unitOfWork.Booking.Update(booking);
                _unitOfWork.Complete();

                return (true, "Alla artiklar togs bort och uthyrningen har avbokats.", -actualDeduction);
            }

            // Validation to check if rental fits in with accommodation booking
            if (booking.Accommodations.Any())
            {
                DateTime stayStart = booking.Accommodations.Min(ba => ba.StartDate).Date;
                DateTime stayEnd = booking.Accommodations.Max(ba => ba.EndDate).Date;
                if (startDate.Date < stayStart || endDate.Date > stayEnd)
                {
                    return (false, $"Hyresperioden måste ligga inom vistelsen {stayStart:yyyy-MM-dd} – {stayEnd:yyyy-MM-dd}.", 0m);
                }
            }

            var newItems = _unitOfWork.EquipmentItem.GetItemsByIds(itemIDs).ToList();
            if (newItems.Count != itemIDs.Count)
            {
                return (false, "En eller flera artiklar kunde inte hittas i databasen.", 0m);
            }

            var currentItemIDs = rental.RentalItems.Select(ri => ri.EquipmentItemID).ToHashSet();
            var removedRentalItems = rental.RentalItems.Where(ri => !itemIDs.Contains(ri.EquipmentItemID)).ToList();
            foreach (var removed in removedRentalItems)
            {
                if (removed.EquipmentItem != null)
                {
                    removed.EquipmentItem.Status = EquipmentStatus.Available;
                }
                rental.RentalItems.Remove(removed);
            }

            var newlyAddedItems = newItems.Where(i => !currentItemIDs.Contains(i.EquipmentItemID)).ToList();
            foreach (var added in newlyAddedItems)
            {
                if (startDate.Date <= DateTime.Today)
                {
                    added.Status = EquipmentStatus.Rented;
                }

                rental.RentalItems.Add(new RentalItem
                {
                    Rental = rental,
                    EquipmentItemID = added.EquipmentItemID,
                    EquipmentItem = added
                });
            }

            var allActiveItems = rental.RentalItems.Select(ri => ri.EquipmentItem).Where(i => i != null).ToList();

            bool requiresLicense = RequiresDriverLicense(allActiveItems!);
            if (requiresLicense && !licenseVerified)
            {
                return (false, "Körkort måste kontrolleras vid skoteruthyrning.", 0m);
            }

            // Calculate new price and price difference
            int days = CountRentalDays(startDate, endDate);
            decimal newTotalPrice = 0m;

            foreach (var item in allActiveItems)
            {
                decimal? itemPrice = CalculateRentalPrice(item!.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days);
                if (itemPrice == null)
                {
                    return (false, $"Artikel {item.ArticleNumber} saknar prislista.", 0m);
                }
                newTotalPrice += itemPrice.Value;
            }

            decimal priceDifference = newTotalPrice - rental.TotalPrice;

            // Updates the rental
            rental.StartDate = startDate.Date;
            rental.EndDate = endDate.Date;
            rental.ReturnDate = endDate.Date;
            rental.TotalPrice = newTotalPrice;
            rental.LicenseVerified = requiresLicense && licenseVerified;

            // Adjustment to price difference
            booking.TotalAmount += priceDifference;

            _unitOfWork.Rental.Update(rental);
            _unitOfWork.Booking.Update(booking);

            if (_unitOfWork.Complete() <= 0)
            {
                return (false, "Kunde inte spara ändringarna i databasen.", 0m);
            }

            return (true, $"Uthyrningen uppdaterad. Nytt hyresbelopp: {newTotalPrice:N2} kr.", priceDifference);
        }

        #endregion

        #region Drop-in rental

        /// <summary>
        /// Registers a drop-in rental over the desk, paid at the register and does not connect to accommodation.
        /// </summary>
        public (bool Success, string Message, int? BookingID) CreateDropInRental(
            int customerID,
            IEnumerable<int> equipmentItemIDs,
            DateTime startDate,
            DateTime endDate,
            bool licenseVerified)
        {
            var itemIDs = equipmentItemIDs?.Distinct().ToList() ?? new List<int>();

            if (customerID <= 0)
            {
                return (false, "En giltig kund måste väljas.", null);
            }

            if (itemIDs.Count == 0)
            {
                return (false, "Välj minst en utrustning att hyra ut.", null);
            }

            if (endDate.Date < startDate.Date || startDate.Date < DateTime.Today)
            {
                return (false, "Ogiltigt datumintervall.", null);
            }

            var items = _unitOfWork.EquipmentItem.GetItemsByIds(itemIDs).ToList();
            if (items.Count != itemIDs.Count)
            {
                return (false, "Kunde inte hitta all vald utrustning.", null);
            }

            var unavailable = items.Where(i => i.Status != EquipmentStatus.Available).ToList();
            if (unavailable.Any())
            {
                return (false, "En eller flera valda artiklar är redan uthyrda.", null);
            }

            bool requiresLicense = RequiresDriverLicense(items);
            if (requiresLicense && !licenseVerified)
            {
                return (false, "Körkort måste kontrolleras vid skoteruthyrning.", null);
            }

            int days = CountRentalDays(startDate, endDate);
            decimal totalPrice = 0m;

            foreach (var item in items)
            {
                decimal? itemPrice = CalculateRentalPrice(item.Equipment?.Prices ?? Enumerable.Empty<EquipmentPriceMatrix>(), days);
                if (itemPrice == null)
                {
                    return (false, $"Artikel {item.ArticleNumber} saknar prislista.", null);
                }
                totalPrice += itemPrice.Value;
            }

            var booking = new Booking
            {
                CustomerID = customerID,
                BookingDate = DateTime.Now,
                BookingStatus = BookingStatus.Confirmed,
                CheckInStatus = CheckInStatus.NotCheckedIn,
                HasCancellationInsurance = false,
                TotalAmount = totalPrice,
                AccommodationListPrice = 0m,
                DiscountRate = 0m,
                DiscountAmount = 0m
            };

            var rental = new Rental
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                ReturnDate = endDate.Date,
                Status = "Active",
                TotalPrice = totalPrice,
                LicenseVerified = requiresLicense && licenseVerified
            };

            foreach (var item in items)
            {
                item.Status = EquipmentStatus.Rented;
                rental.RentalItems.Add(new RentalItem
                {
                    Rental = rental,
                    EquipmentItem = item,
                    EquipmentItemID = item.EquipmentItemID
                });
            }

            booking.Rentals.Add(rental);
            _unitOfWork.Booking.Add(booking);

            if (_unitOfWork.Complete() <= 0)
            {
                return (false, "Kunde inte spara drop-in-uthyrningen.", null);
            }

            return (true, $"Drop-in-uthyrning sparad! Bokningsnummer: #{booking.BookingID}. Att betala: {totalPrice:N2} kr.", booking.BookingID);
        }

        public Customer? SearchCustomer(string searchText)
        {
            return _unitOfWork.Customer.SearchCustomer(searchText);
        }

        #endregion
    }
}