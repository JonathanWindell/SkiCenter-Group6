using DataLayer;

namespace TestLayer
{
    [TestFixture]
    public class DatabaseConnectionTests
    {
        [Test]
        public void DbContext_ShouldConnectToSqlServer_UsingAppSettings()
        {
            // Arrange
            using var context = new SkiCenterDbContext();

            // Act
            // CanConnect() opens a connection and tries to connect. 
            // Returns true if connectionstring is valid.
            bool isConnected = context.Database.CanConnect();

            // Assert
            Assert.That(isConnected, Is.True, "Could not connect to database. Validate that appsettings.json exists and contains correct information.");
        }
    }
}
