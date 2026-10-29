using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class EquipmentUnitTests
    {
        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IEquipmentItemRepository> _mockEquipmentRepo;
        private SkiShopController _controller;

        [SetUp]
        public void Setup()
        {
            // Create mocks for both Unit of Work and the specific repository
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEquipmentRepo = new Mock<IEquipmentItemRepository>();

            // Configure UnitOfWork to return the mocked equipment repository
            _mockUnitOfWork.Setup(u => u.EquipmentItem).Returns(_mockEquipmentRepo.Object);

            // Inject the mocked IUnitOfWork into the controller
            _controller = new SkiShopController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Create / Add Equipment

        /// <summary>
        /// Test for AddEquipmentItem. Verifies that invalid input returns false and does not interact with DB.
        /// </summary>
        [TestCase(0, "AS1", "170")]      // Invalid EquipmentID
        [TestCase(-1, "AS1", "170")]     // Negative EquipmentID
        [TestCase(1, "", "170")]         // Empty articlenumber
        [TestCase(1, "   ", "170")]      // Only whitespace in article number
        [TestCase(1, "AS1", "")]         // Empty size
        [TestCase(1, "AS1", "   ")]      // Only whitespace in size
        public void AddEquipmentItem_InvalidInput_ReturnsFalse(int eqId, string articleNumber, string size)
        {
            // Act
            bool result = _controller.AddEquipmentItem(eqId, articleNumber, size, EquipmentStatus.Available, EquipmentCondition.New);

            // Assert
            Assert.That(result, Is.False);
            _mockEquipmentRepo.Verify(r => r.Add(It.IsAny<EquipmentItem>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for AddEquipmentItem. Verifies that you cant add an article number that already exists.
        /// </summary>
        [Test]
        public void AddEquipmentItem_DuplicateArticleNumber_ReturnsFalse()
        {
            // Arrange
            _mockEquipmentRepo.Setup(r => r.GetByBarcode("AS1"))
                              .Returns(new EquipmentItem { ArticleNumber = "AS1" });

            // Act
            bool result = _controller.AddEquipmentItem(1, "AS1", "170", EquipmentStatus.Available, EquipmentCondition.New);

            // Assert
            Assert.That(result, Is.False);
            _mockEquipmentRepo.Verify(r => r.Add(It.IsAny<EquipmentItem>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for AddEquipmentItem. Verifies that a correct article can be added and saved by UnitOfWork.
        /// </summary>
        [Test]
        public void AddEquipmentItem_ValidInput_ReturnsTrue()
        {
            // Arrange
            _mockEquipmentRepo.Setup(r => r.GetByBarcode("AS100")).Returns((EquipmentItem)null!);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _controller.AddEquipmentItem(1, "AS100", "170", EquipmentStatus.Available, EquipmentCondition.New);

            // Assert
            Assert.That(result, Is.True);
            _mockEquipmentRepo.Verify(r => r.Add(It.Is<EquipmentItem>(e => e.ArticleNumber == "AS100")), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Update Equipment

        /// <summary>
        /// Test for UpdateEquipmentItem. Verifies that invalid data is dismissed.
        /// </summary>
        [TestCase("", 1, "170")]
        [TestCase("AS1", 0, "170")]
        [TestCase("AS1", 1, "")]
        public void UpdateEquipmentItem_InvalidInput_ReturnsFalse(string articleNumber, int eqId, string size)
        {
            // Act
            bool result = _controller.UpdateEquipmentItem(articleNumber, eqId, size, EquipmentStatus.Available, EquipmentCondition.New);

            // Assert
            Assert.That(result, Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for UpdateEquipmentItem. Verifies that a missing item returns false.
        /// </summary>
        [Test]
        public void UpdateEquipmentItem_ItemNotFound_ReturnsFalse()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(0);

            // Act
            bool result = _controller.UpdateEquipmentItem("MISSING", 1, "170", EquipmentStatus.Available, EquipmentCondition.New);

            // Assert
            Assert.That(result, Is.False);
            _mockEquipmentRepo.Verify(r => r.UpdateEquipmentItem("MISSING", 1, "170", EquipmentStatus.Available, EquipmentCondition.New), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        /// <summary>
        /// Test for UpdateEquipmentItem. Verifies that valid input correctly updates properties and calls Complete().
        /// </summary>
        [Test]
        public void UpdateEquipmentItem_ValidInput_UpdatesAndReturnsTrue()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _controller.UpdateEquipmentItem("AS1", 1, "170", EquipmentStatus.Maintenance, EquipmentCondition.Worn);

            // Assert
            Assert.That(result, Is.True);
            _mockEquipmentRepo.Verify(r => r.UpdateEquipmentItem("AS1", 1, "170", EquipmentStatus.Maintenance, EquipmentCondition.Worn), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion

        #region Delete Equipment

        /// <summary>
        /// Test for DeleteEquipmentItem. Verifies that a item with rented status cant be deleted.
        /// </summary>
        [Test]
        public void DeleteEquipmentItem_WhenStatusIsRented_ReturnsFalse()
        {
            // Arrange
            var rentedItem = new EquipmentItem
            {
                ArticleNumber = "AS1",
                Status = EquipmentStatus.Rented
            };
            _mockEquipmentRepo.Setup(r => r.GetByBarcode("AS1")).Returns(rentedItem);

            // Act
            bool result = _controller.DeleteEquipmentItem("AS1");

            // Assert
            Assert.That(result, Is.False);
            _mockEquipmentRepo.Verify(r => r.Delete(It.IsAny<EquipmentItem>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for DeleteEquipmentItem. Verifies that attempting to delete a missing item returns false.
        /// </summary>
        [Test]
        public void DeleteEquipmentItem_ItemNotFound_ReturnsFalse()
        {
            // Arrange
            _mockEquipmentRepo.Setup(r => r.GetByBarcode("MISSING")).Returns((EquipmentItem)null!);

            // Act
            bool result = _controller.DeleteEquipmentItem("MISSING");

            // Assert
            Assert.That(result, Is.False);
            _mockEquipmentRepo.Verify(r => r.Delete(It.IsAny<EquipmentItem>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        /// <summary>
        /// Test for DeleteEquipmentItem. Verifies that an available article is deleted and Complete() is called.
        /// </summary>
        [Test]
        public void DeleteEquipmentItem_ValidAvailableItem_ReturnsTrue()
        {
            // Arrange
            var availableItem = new EquipmentItem
            {
                ArticleNumber = "AS1",
                Status = EquipmentStatus.Available
            };
            _mockEquipmentRepo.Setup(r => r.GetByBarcode("AS1")).Returns(availableItem);
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1);

            // Act
            bool result = _controller.DeleteEquipmentItem("AS1");

            // Assert
            Assert.That(result, Is.True);
            _mockEquipmentRepo.Verify(r => r.Delete(availableItem), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        #endregion
    }
}