using BusinessLayer.Controllers;

namespace TestLayer.UnitTests
{
    public class CustomerUnitTests
    {
        [TestFixture]
        [Category("Unit")]
        public class CustomerValidationTests
        {

            /// <summary>
            /// Verifies that when Private customer data is missing no data is written to the database. 
            /// </summary>
            [TestCase("", "", "", "CarlSvensson@gmail.se", "070111")]
            [TestCase("Carl", "", "GöteborgsGatan 1", "CarlSvensson@gmail.se", "070111")]
            [TestCase("Carl", "Svensson", "", "CarlSvensson@gmail.se", "070111")]
            public void RegisterPrivateCustomer_EmptyFields_ReturnsFalse(
                string firstName, string lastName, string address, string email, string phone)
            {
                // Arrange
                var controller = new CustomerController(unitOfWork: null);

                // Act
                bool result = controller.RegisterPrivateCustomer(firstName, lastName, address, email, phone);

                // Assert
                Assert.That(result, Is.False);
            }

            /// <summary>
            /// Verifies that when Corporate customer data is missing no data is written to the database. 
            /// </summary>
            [TestCase("", "345689-3454", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")]
            [TestCase("", "345689-3454", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")]
            [TestCase("", "345689-3454", "Mark Johnson", "Dietmar-Hopp-Allee 16", "SAP@gmail.com", "0761678943")]
            public void RegisterCorporateCustomer_EmptyFields_ReturnsFalse(
                string companyName, string organisationNumber, string contactPerson, string address, string email, string phoneNumber)
            {
                // Arrange
                var controller = new CustomerController(unitOfWork: null);

                // Act
                bool result = controller.RegisterCorporateCustomer(companyName, organisationNumber, contactPerson, address, email, phoneNumber);

                // Assert
                Assert.That(result, Is.False);
            }
        }
    }
}
