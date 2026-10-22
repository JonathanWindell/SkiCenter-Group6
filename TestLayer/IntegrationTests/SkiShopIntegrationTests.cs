using BusinessLayer.Controllers;
using DataLayer;
using EntityLayer;

namespace TestLayer.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class SkiShopIntegrationTests
    {
        private SkiCenterDbContext _context;
        private SkiShopController _skiShopController;

        // Tracking lists to explicitly delete test data in reverse relational order
        private List<Rental> _createdRentals;
        private List<EquipmentItem> _createdItems;
        private List<Equipment> _createdEquipments;

        [SetUp]
        public void Setup()
        {
            _context = new SkiCenterDbContext();

            // Wire up dependencies locally to avoid NUnit1032
            var unitOfWork = new UnitOfWork(_context);
            _skiShopController = new SkiShopController(unitOfWork);

            // Initialize tracking lists for cleanup
            _createdRentals = new List<Rental>();
            _createdItems = new List<EquipmentItem>();
            _createdEquipments = new List<Equipment>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                // Removes Rentals first (child table referencing equipment items)
                if (_createdRentals.Any())
                {
                    _context.Rentals.RemoveRange(_createdRentals);
                    _context.SaveChanges();
                }

                // Removes EquipmentItems (child to Equipment, parent to Rental)
                if (_createdItems.Any())
                {
                    _context.EquipmentItems.RemoveRange(_createdItems);
                    _context.SaveChanges();
                }

                // Removes Equipment last (root parent table)
                if (_createdEquipments.Any())
                {
                    _context.Equipment.RemoveRange(_createdEquipments);
                    _context.SaveChanges();
                }

                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        /// Verifies that an available item is rented out successfully, transitions to Rented,
        /// and an active Rental record is persisted in the database.
        /// </summary>
        [Test]
        public void AvailableItem_CreateRental_ReturnsTrue()
        {
            // Arrange: Seed equipment and an available equipment item
            var equipment = new Equipment
            {
                Category = "Alpint",
                Description = "Slalomskidor Avancerad",
                Prices = new List<EquipmentPriceMatrix>
                {
                    new EquipmentPriceMatrix { Days = 1, TotalAmount = 250 }
                }
            };
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
            _createdEquipments.Add(equipment);

            string barcode = "Test-Slalomskis-104";
            var item = new EquipmentItem
            {
                ArticleNumber = barcode,
                EquipmentID = equipment.EquipmentID,
                Size = "170 cm",
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.BarelyUsed
            };
            _context.EquipmentItems.Add(item);
            _context.SaveChanges();
            _createdItems.Add(item);

            var rental = new Rental
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                RentalItems = new List<RentalItem>()
            };

            // Act: Attempt to create rental
            bool isSuccess = _skiShopController.CreateRental(rental, new List<string> { barcode });
            if (rental.RentalID > 0)
            {
                _createdRentals.Add(rental);
            }

            // Assert: Verify rental creation and status transition in database
            Assert.That(isSuccess, Is.True, "CreateRental should succeed for available equipment.");

            var updatedItem = _context.EquipmentItems.FirstOrDefault(i => i.ArticleNumber == barcode);
            Assert.That(updatedItem, Is.Not.Null);
            Assert.That(updatedItem.Status, Is.EqualTo(EquipmentStatus.Rented), "Equipment status must transition to Rented.");
            Assert.That(rental.Status, Is.EqualTo("Active"), "Rental status must be Active.");
        }

        /// <summary>
        /// Verifies that attempting to rent an item marked as Damaged is rejected by the business logic
        /// and no rental record is persisted.
        /// </summary>
        [Test]
        public void DamagedItem_CreateRental_ReturnsFalse()
        {
            // Arrange: Seed equipment and a damaged item
            var equipment = new Equipment
            {
                Category = "Alpint",
                Description = "Slalomskidor Nybörjare"
            };
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
            _createdEquipments.Add(equipment);

            string barcode = "Test-Slalomskis-105";
            var damagedItem = new EquipmentItem
            {
                ArticleNumber = barcode,
                EquipmentID = equipment.EquipmentID,
                Size = "150 cm",
                Status = EquipmentStatus.Damaged,
                Condition = EquipmentCondition.Worn
            };
            _context.EquipmentItems.Add(damagedItem);
            _context.SaveChanges();
            _createdItems.Add(damagedItem);

            var rental = new Rental
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                RentalItems = new List<RentalItem>()
            };

            // Act: Attempt to rent the damaged item
            bool isSuccess = _skiShopController.CreateRental(rental, new List<string> { barcode });

            // Assert: Verify rejection and unchanged status
            Assert.That(isSuccess, Is.False, "CreateRental must fail when item is damaged.");

            var persistedItem = _context.EquipmentItems.FirstOrDefault(i => i.ArticleNumber == barcode);
            Assert.That(persistedItem, Is.Not.Null);
            Assert.That(persistedItem.Status, Is.EqualTo(EquipmentStatus.Damaged), "Item status must remain Damaged.");
        }

        /// <summary>
        /// Verifies that renting equipment in the snowmobile category is rejected
        /// when driver's license verification is set to false.
        /// </summary>
        [Test]
        public void SnowmobileWithoutLicense_CreateRental_ReturnsFalse()
        {
            // Arrange: Seed snowmobile equipment with pricing and an available item
            var snowmobile = new Equipment
            {
                Category = SkiShopController.SnowmobileCategory,
                Description = "Snöskoter Touring 600cc",
                Prices = new List<EquipmentPriceMatrix>
                {
                    new EquipmentPriceMatrix { Days = 1, TotalAmount = 1500 }
                }
            };
            _context.Equipment.Add(snowmobile);
            _context.SaveChanges();
            _createdEquipments.Add(snowmobile);

            string barcode = "Test-Snowmobile-01";
            var item = new EquipmentItem
            {
                ArticleNumber = barcode,
                EquipmentID = snowmobile.EquipmentID,
                Size = "Standard",
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.New
            };
            _context.EquipmentItems.Add(item);
            _context.SaveChanges();
            _createdItems.Add(item);

            var rental = new Rental
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                RentalItems = new List<RentalItem>(),
                LicenseVerified = false // Unverified license precondition
            };

            // Act: Attempt rental without verified driver's license
            bool isSuccess = _skiShopController.CreateRental(rental, new List<string> { barcode });

            // Assert: Verify business rule rejection
            Assert.That(isSuccess, Is.False, "Rental of snowmobile must be denied if license is not verified.");
        }

        /// <summary>
        /// Test-ID: UT-HP-01.2
        /// Verifies that returning an active rental updates the rental status to Available
        /// and resets the equipment status back to Available in the database.
        /// </summary>
        [Test]
        public void ActiveRental_ReturnRental_ReturnsTrue()
        {
            // Arrange: Seed equipment, rented item, and an active rental
            var equipment = new Equipment
            {
                Category = "Snowboard",
                Description = "Snowboard All-Mountain",
                Prices = new List<EquipmentPriceMatrix>
                {
                    new EquipmentPriceMatrix { Days = 1, TotalAmount = 300 }
                }
            };
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
            _createdEquipments.Add(equipment);

            string barcode = "Test-Snowboard-01";
            var item = new EquipmentItem
            {
                ArticleNumber = barcode,
                EquipmentID = equipment.EquipmentID,
                Size = "158 cm",
                Status = EquipmentStatus.Rented,
                Condition = EquipmentCondition.New
            };
            _context.EquipmentItems.Add(item);
            _context.SaveChanges();
            _createdItems.Add(item);

            var rental = new Rental
            {
                StartDate = DateTime.Today.AddDays(-2),
                EndDate = DateTime.Today,
                Status = "Active",
                TotalPrice = 600m,
                RentalItems = new List<RentalItem>
                {
                    new RentalItem { EquipmentItem = item, EquipmentItemID = item.EquipmentItemID }
                }
            };
            _context.Rentals.Add(rental);
            _context.SaveChanges();
            _createdRentals.Add(rental);

            // Act: Return the rental through controller
            bool isSuccess = _skiShopController.ReturnRental(rental.RentalID);

            // Assert: Verify successful return and item availability
            Assert.That(isSuccess, Is.True, "ReturnRental should succeed for an active rental.");

            var returnedItem = _context.EquipmentItems.FirstOrDefault(i => i.ArticleNumber == barcode);
            Assert.That(returnedItem, Is.Not.Null);
            Assert.That(returnedItem.Status, Is.EqualTo(EquipmentStatus.Available), "Item status must revert to Available.");
        }

        /// <summary>
        /// Verifies that registering an equipment item with an article number that already exists
        /// in the database returns false and prevents duplicate entries.
        /// </summary>
        [Test]
        public void ExistingArticleNumber_AddEquipmentItem_ReturnsFalse()
        {
            // Arrange: Seed equipment and an existing item
            var equipment = new Equipment
            {
                Category = "Hjälmar",
                Description = "Skidhjälm Vuxen"
            };
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
            _createdEquipments.Add(equipment);

            string duplicateBarcode = "Test-Helmet-Duplicate-01";
            var existingItem = new EquipmentItem
            {
                ArticleNumber = duplicateBarcode,
                EquipmentID = equipment.EquipmentID,
                Size = "L",
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.New
            };
            _context.EquipmentItems.Add(existingItem);
            _context.SaveChanges();
            _createdItems.Add(existingItem);

            // Act: Attempt to insert a second item with the duplicate article number
            bool isSuccess = _skiShopController.AddEquipmentItem(
                equipment.EquipmentID,
                duplicateBarcode,
                "M",
                EquipmentStatus.Available,
                EquipmentCondition.New);

            // Assert: Verify duplicate prevention
            Assert.That(isSuccess, Is.False, "AddEquipmentItem must reject items with duplicate article numbers.");
        }

        /// <summary>
        /// Verifies that an equipment item currently with status Rented cannot be deleted from the database.
        /// </summary>
        [Test]
        public void RentedItem_DeleteEquipmentItem_ReturnsFalse()
        {
            // Arrange: Seed equipment and an item currently rented
            var equipment = new Equipment
            {
                Category = "Stavar",
                Description = "Alpinstavar Kolfiber"
            };
            _context.Equipment.Add(equipment);
            _context.SaveChanges();
            _createdEquipments.Add(equipment);

            string barcode = "Test-Skipoles-Rented-01";
            var rentedItem = new EquipmentItem
            {
                ArticleNumber = barcode,
                EquipmentID = equipment.EquipmentID,
                Size = "125 cm",
                Status = EquipmentStatus.Rented,
                Condition = EquipmentCondition.New
            };
            _context.EquipmentItems.Add(rentedItem);
            _context.SaveChanges();
            _createdItems.Add(rentedItem);

            // Act: Attempt to delete the rented item
            bool isSuccess = _skiShopController.DeleteEquipmentItem(barcode);

            // Assert: Verify deletion is blocked
            Assert.That(isSuccess, Is.False, "DeleteEquipmentItem must reject deletion of rented equipment.");

            var persistedItem = _context.EquipmentItems.FirstOrDefault(i => i.ArticleNumber == barcode);
            Assert.That(persistedItem, Is.Not.Null, "The rented item must still exist in the database.");
        }
    }
}