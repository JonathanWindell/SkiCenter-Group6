using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class SkiShopController
    {
        private readonly IUnitOfWork _unitOfWork;

        public SkiShopController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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
        /// /// <param name="category"/param>
        public IEnumerable<Equipment> GetEquipmentByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<Equipment>();
            }

            return _unitOfWork.Equipment.GetByCategory(category);
        }

        /// <summary>
        /// Gets a specific category when staff scans/input the article number.
        /// </summary>
        /// /// <param name="articleNumber"/param>
        public EquipmentItem FindItemByBarcode(string articleNumber)
        {
            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                return null;
            }

            return _unitOfWork.EquipmentItem.GetByBarcode(articleNumber);
        }

        /// <summary>
        /// Gets all available items of a specific model.
        /// </summary>
        /// /// <param name="equipmentID"/param>
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
        /// /// <param name="articleNumber"/param>
        /// /// <param name="status"/param>
        /// /// <param name="condition"/param>
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
        /// /// <param name="rental"/param>
        /// /// <param name="scannedArticleNumbers"/param>
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

            // Adds scanned items to the rental
            foreach (var barcode in scannedArticleNumbers)
            {
                var item = _unitOfWork.EquipmentItem.GetByBarcode(barcode);
                if (item != null && item.Status == EquipmentStatus.Available)
                {
                    rental.EquipmentItems.Add(item);
                    _unitOfWork.EquipmentItem.UpdateStatusAndCondition(barcode, EquipmentStatus.Rented, item.Condition);
                }
            }

            if (!rental.EquipmentItems.Any())
            {
                return false;
            }

            rental.Status = "Active";
            _unitOfWork.Rental.Add(rental);

            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Returns a rental or scanned articles and make them available again.
        /// </summary>
        /// /// <param name="rentalID"/param>
        public bool ReturnRental(int rentalID)
        {
            var rental = _unitOfWork.Rental.GetRentalWithItems(rentalID);
            if (rental == null || rental.Status == "Returned")
            {
                return false;
            }

            // Returns status to available
            foreach (var item in rental.EquipmentItems)
            {
                _unitOfWork.EquipmentItem.UpdateStatusAndCondition(item.ArticleNumber, EquipmentStatus.Available, item.Condition);
            }

            rental.Status = "Available";
            rental.ReturnDate = DateTime.Now;

            _unitOfWork.Rental.Update(rental);
            return _unitOfWork.Complete() > 0;
        }

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

            // Only available equipment may be rented out
            List<EquipmentItem> unavailable = items.Where(item => item.Status != EquipmentStatus.Available).ToList();
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

            var rental = new Rental
            {
                BookingID = bookingID,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                // Planned return date. Updated with the actual date when the rental is returned.
                ReturnDate = endDate.Date,
                Status = "Active",
                TotalPrice = totalPrice,
                LicenseVerified = requiresLicense && licenseVerified
            };

            foreach (EquipmentItem item in items)
            {
                item.Status = EquipmentStatus.Rented;
                rental.EquipmentItems.Add(item);
            }

            // Add the rental price to the booking's total amount
            Booking booking = _unitOfWork.Booking.GetByID(bookingID);
            booking.TotalAmount += totalPrice;

            _unitOfWork.Rental.Add(rental);

            if (_unitOfWork.Complete() <= 0)
            {
                return (false, "Uthyrningen kunde inte sparas.", 0m);
            }

            return (true,
                $"{items.Count} utrustning(ar) har lagts till på bokning {bookingID} för {days} dag(ar). " +
                $"{totalPrice:N2} kr har lagts till på bokningen. Nytt totalbelopp: {booking.TotalAmount:N2} kr.",
                totalPrice);
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
    }
}