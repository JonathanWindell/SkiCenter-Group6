using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class SkiShopController // WIP
    {
        private readonly IUnitOfWork _unitOfWork;

        public SkiShopController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets all equipment types with their current stock.
        /// </summary>
        public IEnumerable<Equipment> GetAllEquipmentWithStock()
        {
            return _unitOfWork.Equipment.GetAllEquipmentWithStock();
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
        public bool UpdateItemConditionAndStatus(string articleNumber, EquipmentStatus status, EquipmentCondition condition)
        {
            if (string.IsNullOrWhiteSpace(articleNumber))
            {
                return false;
            }

            _unitOfWork.EquipmentItem.UpdateStatusAndCondition(articleNumber, status, condition);
            return _unitOfWork.Complete() > 0;
        }

        /// <summary>
        /// Register a new item for the ski shop.
        /// </summary>
        /// <param name="equipmentId"/param>
        /// <param name="articleNumber"/param>
        /// <param name="size"/param>
        /// <param name="pricePerDay"/param>
        /// <param name="condition"/param>
        /// <returns>True if the add was successful, otherwise false.</returns>
        public bool AddEquipmentItem(int equipmentId, string articleNumber, string size, decimal pricePerDay, EquipmentCondition condition = EquipmentCondition.New)
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
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.New,
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
    }
}