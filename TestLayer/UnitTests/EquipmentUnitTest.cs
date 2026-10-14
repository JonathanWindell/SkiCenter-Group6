using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{

    [TestFixture]
    [Category("Unit")]
    public class EquipmentUnitTest
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IEquipmentItemRepository> _mockEquipmentItemRepo;
        private Mock<IEquipmentRepository> _mockEquipmentRepo;
        private Mock<IRentalRepository> _mockRentalRepo;

        private SkiShopController _controller;

        [SetUp]
        public void Setup()
        {
            // Create mocks for both Unit of Work and the specific repository
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEquipmentItemRepo = new Mock<IEquipmentItemRepository>();
            _mockEquipmentRepo = new Mock<IEquipmentRepository>();
            _mockRentalRepo = new Mock<IRentalRepository>();

            // Configure UnitOfWork to return the mocked booking repository
            _mockUnitOfWork.Setup(u => u.EquipmentItem).Returns(_mockEquipmentItemRepo.Object);
            _mockUnitOfWork.Setup(u => u.Equipment).Returns(_mockEquipmentRepo.Object);
            _mockUnitOfWork.Setup(u => u.Rental).Returns(_mockRentalRepo.Object);

            // Inject the mocked IUnitOfWork into the controller
            _controller = new SkiShopController(_mockUnitOfWork.Object);
        }

        /// <summary>
        /// Verifies that when a Category that does not exist is queried to the database an empty result is returned. 
        /// </summary>
        [Test]
        public void GetEquipmentByCategory_NonValidData_ReturnsEmpty()
        {
            // Arrange
            string fakeCategory = "NoSkis";
            _mockEquipmentRepo
                .Setup(repo => repo.GetByCategory(fakeCategory))
                .Returns(new List<Equipment>());

            // Act
            var result = _controller.GetEquipmentByCategory(fakeCategory);

            // Assert
            Assert.That(result, Is.Empty, "Expected empty collection when category is not correct.");

            _mockEquipmentRepo.Verify(e => e.GetByCategory(fakeCategory), Times.Once);
        }

        /// <summary>
        /// Verifies that when a non valid EquipmentID is entered an empty result is returned. 
        /// </summary>
        [Test]
        public void GetAvailableItems_NonValidData_ReturnsEmpty()
        {
            // Arrange
            int nonValidEquipmentID = -5;

            // Act
            var result = _controller.GetAvailableItems(nonValidEquipmentID);

            // Assert
            Assert.That(result, Is.Empty, "Expected empty collection when category is not correct.");

            _mockEquipmentItemRepo.Verify(e => e.GetAvailableItems(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Verifies that returned Rental equipment is assigned correct status again. 
        /// </summary>
        [Test]
        public void ReturnRental_ValidData_ReturnsTrue()
        {
            // Arrange
            int rentalID = 1;
            string rentalStatus = "Returned";

            var testRental = new Rental
            {
                RentalID = rentalID,
                Status = "Active",
                EquipmentItems = new List<EquipmentItem>
        {
            new EquipmentItem
            {
                ArticleNumber = "AS1",
                Status = EquipmentStatus.Rented,
                Condition = EquipmentCondition.New
            }
        }
            };

            // Mock return of rental
            _mockRentalRepo
                .Setup(repo => repo.GetRentalWithItems(rentalID))
                .Returns(testRental);

            // Mock completed transaction
            _mockUnitOfWork
                .Setup(uow => uow.Complete())
                .Returns(1);

            // Act
            var result = _controller.ReturnRental(rentalID);

            // Assert
            Assert.That(result, Is.True, "ReturnRental should return true.");
            Assert.That(testRental.Status, Is.EqualTo("Available"));

            // Verify
            _mockEquipmentItemRepo.Verify(repo =>
                repo.UpdateStatusAndCondition("AS1", EquipmentStatus.Available, EquipmentCondition.New),
                Times.Once);

            _mockUnitOfWork.Verify(uow => uow.Complete(), Times.Once);
        }



    }
}
