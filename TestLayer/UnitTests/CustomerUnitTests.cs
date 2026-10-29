using BusinessLayer.Controllers;
using DataLayer.Interfaces;
using EntityLayer;
using Moq;

namespace TestLayer.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class CustomerUnitTests
    {
        #region Setup & Mocks

        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ICustomerRepository> _mockCustomerRepo;
        private CustomerController _controller;

        [SetUp]
        public void Setup()
        {
            // Create mocks for database
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();

            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);

            // Inject controller 
            _controller = new CustomerController(_mockUnitOfWork.Object);
        }

        #endregion

        #region Private Customer

        /// <summary>
        /// Verifies that when Private customer data is missing no data is written to the database. 
        /// </summary>
        [TestCase("", "", "", "CarlSvensson@gmail.se", "070111")]
        [TestCase("Carl", "", "GöteborgsGatan 1", "CarlSvensson@gmail.se", "070111")]
        [TestCase("Carl", "Svensson", "", "CarlSvensson@gmail.se", "070111")]
        public void RegisterPrivateCustomer_EmptyFields_ReturnsFalse(
            string firstName, string lastName, string address, string email, string phone)
        {
            // Act
            bool result = _controller.RegisterPrivateCustomer(firstName, lastName, address, email, phone);

            // Assert
            Assert.That(result, Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never, "Databasen ska inte anropas vid ogiltig data.");
        }

        /// <summary>
        /// Verifies that valid private customer data is successfully saved to the database.
        /// </summary>
        [Test]
        public void RegisterPrivateCustomer_ValidData_ReturnsTrue()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1); // Simulate that database saves successfully

            // Act
            bool result = _controller.RegisterPrivateCustomer("Carl", "Svensson", "GöteborgsGatan 1", "Carl@gmail.com", "0701234567");

            // Assert
            Assert.That(result, Is.True);
            _mockCustomerRepo.Verify(r => r.Add(It.IsAny<PrivateCustomer>()), Times.Once, "Kunden lades inte till i repositoryt.");
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once, "Databas-transaktionen slutfördes inte.");
        }

        #endregion

        #region Corporate Customer

        /// <summary>
        /// Verifies that when Corporate customer data is missing no data is written to the database. 
        /// </summary>
        [TestCase("", "345689-3454", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")]
        [TestCase("SAP", "", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")] // Empty org-number
        [TestCase("SAP", "345689-3454", "", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")] // Empty contact person
        public void RegisterCorporateCustomer_EmptyFields_ReturnsFalse(
            string companyName, string organisationNumber, string contactPerson, string address, string email, string phoneNumber)
        {
            // Act
            bool result = _controller.RegisterCorporateCustomer(companyName, organisationNumber, contactPerson, address, email, phoneNumber);

            // Assert
            Assert.That(result, Is.False);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never, "Databasen ska inte anropas vid ogiltig data.");
        }

        /// <summary>
        /// Verifies that valid corporate customer data is successfully saved to the database.
        /// </summary>
        [Test]
        public void RegisterCorporateCustomer_ValidData_ReturnsTrue()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Complete()).Returns(1); // Simulate that database saves successfully

            // Act
            bool result = _controller.RegisterCorporateCustomer("SAP", "345689-3454", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943");

            // Assert
            Assert.That(result, Is.True);
            _mockCustomerRepo.Verify(r => r.Add(It.IsAny<CorporateCustomer>()), Times.Once, "Företagskunden lades inte till i repositoryt.");
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once, "Databas-transaktionen slutfördes inte.");
        }

        #endregion
    }
}