using DataLayer;
using EntityLayer;

namespace TestLayer.IntegrationTests
{
    public class RentalIntegrationTests
    {
        private SkiCenterDbContext _context;

        [SetUp]
        public void Setup()
        {
            // Initialize database. 
            _context = new SkiCenterDbContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure the DbContext is disposed after each test to avoid resource leaks
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        //Integrate Businesslayer
        [Test]
        public void SkiRental_BookAvailableSkis()
        {
            // Arrange
            // Create equipment category. (Needed for foreign key)
            var skiCategory = new Equipment
            {
                Description = "Slalom Carving",
                Category = "Skidor"
            };
            _context.Equipment.Add(skiCategory);
            _context.SaveChanges(); // Save Equipment 

            // Create EquipmentItem and assign to category
            var skiItem = new EquipmentItem
            {
                ArticleNumber = "AS365",
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.New,
                Size = "175cm",
                EquipmentID = skiCategory.EquipmentID
            };
            _context.EquipmentItems.Add(skiItem);
            _context.SaveChanges();

            // Act
            var rental = new Rental
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(3),
                Status = "Aktiv",
                Sum = 750m
            };

            rental.EquipmentItems.Add(skiItem);
            skiItem.Status = EquipmentStatus.Rented; // Update status

            _context.Rentals.Add(rental);
            _context.SaveChanges();

            // Assert
            // Fetch equipment and include items
            var savedRental = _context.Rentals
                .FirstOrDefault(r => r.RentalID == rental.RentalID);

            Assert.That(savedRental, Is.Not.Null);
            Assert.That(savedRental.RentalID, Is.GreaterThan(0));
            Assert.That(skiItem.Status, Is.EqualTo(EquipmentStatus.Rented));

            // Clean up testdata
            _context.Rentals.Remove(rental);
            _context.EquipmentItems.Remove(skiItem);
            _context.Equipment.Remove(skiCategory);
            _context.SaveChanges();
        }

        //Integrate Businesslayer
        [Test]
        public void SkiRental_BookDamagedSkis()
        {
            // Arrange
            // Create equipment category. (Needed for foreign key)
            var skiCategory = new Equipment
            {
                Description = "Slalom Carving",
                Category = "Skidor"
            };
            _context.Equipment.Add(skiCategory);
            _context.SaveChanges(); // Save Equipment 

            // Create EquipmentItem and assign to category
            var skiItem = new EquipmentItem
            {
                ArticleNumber = "AS365",
                Status = EquipmentStatus.Available,
                Condition = EquipmentCondition.New,
                Size = "175cm",
                EquipmentID = skiCategory.EquipmentID
            };
            _context.EquipmentItems.Add(skiItem);
            _context.SaveChanges();

            // Act
            var rental = new Rental
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(3),
                Status = "Aktiv",
                Sum = 750m
            };

            rental.EquipmentItems.Add(skiItem);
            skiItem.Status = EquipmentStatus.Damaged; // Update status

            _context.Rentals.Add(rental);
            _context.SaveChanges();

            // Assert
            // Fetch equipment and include items
            var savedRental = _context.Rentals
                .FirstOrDefault(r => r.RentalID == rental.RentalID);

            Assert.That(savedRental, Is.Not.Null);
            Assert.That(savedRental.RentalID, Is.GreaterThan(0));
            Assert.That(skiItem.Status, Is.EqualTo(EquipmentStatus.Damaged));

            // Clean up testdata
            _context.Rentals.Remove(rental);
            _context.EquipmentItems.Remove(skiItem);
            _context.Equipment.Remove(skiCategory);
            _context.SaveChanges();

        }

        //Integrate Businesslayer
        [Test]
        public void AccomodationRental_BookAvailableApartment()
        {


        }

        //Integrate Businesslayer
        [Test]
        public void AccomodationRental_BookOccupiedApartment()
        {
            // Arrange
            /*
             * Create temp booking with todays date, arrival date (75 days ahead), total sum
             */

            // Act
            /*
             * Save booking to database
             * Fetch BusinessLayer logic regarding payment plans
             */

            // Assert
            /*
             * Payment plan is generated correctly based on payment plan rules. 
             */

            /*
            // Remove test booking
            */

        }
    }
}
