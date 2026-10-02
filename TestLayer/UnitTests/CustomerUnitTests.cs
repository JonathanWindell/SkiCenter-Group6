using BusinessLayer.Controllers;

namespace TestLayer.UnitTests
{
    public class CustomerUnitTests
    {
        [TestFixture]
        [Category("Unit")]
        public class CustomerValidationTests
        {
            [TestCase("", "Svensson", "Gatan 1", "test@test.se", "070111")]
            [TestCase("Kalle", "", "Gatan 1", "test@test.se", "070111")]
            [TestCase("Kalle", "Svensson", "", "test@test.se", "070111")]
            public void RegisterPrivateCustomer_EmptyFields_ReturnsFalseImmediately(
                string firstName, string lastName, string address, string email, string phone)
            {
                // Arrange
                var controller = new CustomerController(unitOfWork: null);

                // Act
                bool result = controller.RegisterPrivateCustomer(firstName, lastName, address, email, phone);

                // Assert
                Assert.That(result, Is.False);
            }
        }
    }
}
